using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Infrastructure.Identity;
using LocalizadorCafes.Infrastructure.Osm;
using LocalizadorCafes.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using LocalizadorCafes.Infrastructure.Persistence;
using LocalizadorCafes.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LocalizadorCafes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useInMemory = false,
        string? uploadsPath = null)
    {
        services.AddSingleton(new LocalPhotoStorage(
            uploadsPath ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads")));
        services.AddSingleton<IPhotoStorage>(provider => provider.GetRequiredService<LocalPhotoStorage>());

        // OpenStreetMap exige identificar la aplicación con un User-Agent propio.
        services.AddHttpClient<IOsmCafeSource, OsmCafeSource>(client =>
        {
            client.BaseAddress = new Uri("https://api.openstreetmap.org/api/0.6/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LocalizadorCafes/1.0");
        });

        if (useInMemory)
        {
            // Sin PostgreSQL la API sigue siendo funcional con datos de ejemplo.
            // Una sola instancia sirve a la web y al panel de administración.
            var memory = new InMemoryCafeRepository(DemoData.CreateCafes());
            services.AddSingleton<ICafeRepository>(memory);
            services.AddSingleton<IAdminRepository>(memory);
            services.AddSingleton<IAuditLog, InMemoryAuditLog>();

            return services;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'DefaultConnection'.");

        // La fuente de datos se construye a mano para registrar el plugin de
        // PostGIS (NetTopologySuite). Si se pasa solo la cadena de conexión,
        // Npgsql crea una fuente sin el plugin y falla al escribir `geography`.
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseNetTopologySuite();
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                serviceProvider.GetRequiredService<NpgsqlDataSource>(),
                npgsql =>
                {
                    npgsql.UseNetTopologySuite();
                    npgsql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<ICafeRepository, CafeRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAuditLog, EfAuditLog>();

        return services;
    }

    /// <summary>
    /// Almacén de cuentas: las tablas AspNet* de PostgreSQL o, en modo demo,
    /// una base de datos en memoria que se vacía al reiniciar.
    /// </summary>
    public static IdentityBuilder AddIdentityStores(
        this IdentityBuilder builder,
        bool useInMemory)
    {
        if (useInMemory)
        {
            builder.Services.AddDbContext<InMemoryIdentityDbContext>(options =>
                options.UseInMemoryDatabase("localizador-cafes-cuentas"));

            return builder.AddEntityFrameworkStores<InMemoryIdentityDbContext>();
        }

        return builder.AddEntityFrameworkStores<AppDbContext>();
    }
}

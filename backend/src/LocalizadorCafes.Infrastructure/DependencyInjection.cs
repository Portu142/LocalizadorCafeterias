using LocalizadorCafes.Application.Common.Interfaces;
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
        bool useInMemory = false)
    {
        if (useInMemory)
        {
            // Sin PostgreSQL la API sigue siendo funcional con datos de ejemplo.
            services.AddSingleton<ICafeRepository>(_ =>
                new InMemoryCafeRepository(DemoData.CreateCafes()));

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

        return services;
    }
}

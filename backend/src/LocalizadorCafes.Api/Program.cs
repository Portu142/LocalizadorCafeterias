using System.Threading.RateLimiting;
using LocalizadorCafes.Api.Middleware;
using LocalizadorCafes.Application;
using LocalizadorCafes.Infrastructure;
using LocalizadorCafes.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// La API puede arrancar sin PostgreSQL: si la base de datos no responde se
// usa un repositorio en memoria con los datos de ejemplo (modo demo).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var allowInMemoryFallback = builder.Configuration.GetValue("Database:AllowInMemoryFallback", true);
var databaseAvailable = !EF.IsDesignTime &&
    !string.IsNullOrWhiteSpace(connectionString) &&
    await CanConnectAsync(connectionString!);
var useInMemory = !EF.IsDesignTime && !databaseAvailable && allowInMemoryFallback;

if (useInMemory)
{
    Console.WriteLine(
        "\n⚠  No se pudo conectar con PostgreSQL. La API arranca en MODO DEMO con datos en memoria.\n" +
        "   Las reseñas nuevas se pierden al reiniciar. Para usar PostgreSQL:\n" +
        "     .\\scripts\\start-db.ps1   (PostgreSQL en WSL)\n" +
        "     docker compose up -d db           (Docker)\n" +
        "   o ajusta ConnectionStrings__DefaultConnection.\n");
}

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, useInMemory);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Localizador de Cafés API",
        Version = "v1",
        Description = "API para buscar cafeterías, consultar menús y gestionar reseñas."
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins = new[]
            {
                "http://localhost:5173",
                "http://localhost:4173",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:4173"
            };
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("reviews", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

if (builder.Configuration.GetValue<bool>("ReverseProxy:TrustForwardedHeaders"))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var healthChecks = builder.Services.AddHealthChecks();

if (useInMemory)
{
    healthChecks.AddCheck(
        "database",
        () => HealthCheckResult.Healthy("Modo demo con datos en memoria"));
}
else
{
    healthChecks.AddDbContextCheck<AppDbContext>();
}

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Configuration.GetValue<bool>("ReverseProxy:TrustForwardedHeaders"))
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");

if (!useInMemory && !EF.IsDesignTime && app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbInitializer.SeedAsync(dbContext);
}

app.Run();

static async Task<bool> CanConnectAsync(string connectionString)
{
    try
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(timeout.Token);
        return true;
    }
    catch
    {
        return false;
    }
}

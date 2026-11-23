using System.Threading.RateLimiting;
using LocalizadorCafes.Api.Auth;
using LocalizadorCafes.Api.Middleware;
using LocalizadorCafes.Application;
using LocalizadorCafes.Infrastructure;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
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

// Fotos subidas y claves de Data Protection (cifran la cookie de sesión).
// Persistir las claves evita que todas las sesiones caduquen al reiniciar.
var appDataPath = builder.Configuration["Storage:AppDataPath"] is { Length: > 0 } configuredPath
    ? Path.GetFullPath(configuredPath, builder.Environment.ContentRootPath)
    : Path.Combine(builder.Environment.ContentRootPath, "App_Data");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration,
    useInMemory,
    Path.Combine(appDataPath, "uploads"));

builder.Services.AddDataProtection()
    .SetApplicationName("LocalizadorCafes")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(appDataPath, "keys")));

// Cuentas con ASP.NET Identity y sesión en cookie HttpOnly.
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddIdentityStores(useInMemory);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "lc_session";
    options.Cookie.HttpOnly = true;
    // Lax: el navegador no envía la cookie en peticiones POST desde otros
    // sitios, lo que protege frente a CSRF sin tokens adicionales.
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    // La API responde con códigos HTTP; solo el panel /admin (páginas HTML)
    // redirige a su propia pantalla de inicio de sesión.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            context.Response.Redirect(
                "/admin/login?returnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            context.Response.Redirect("/admin/login?denegado=1");
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        return Task.CompletedTask;
    };
});

// La cookie se revalida cada minuto contra el sello de seguridad: al bloquear
// una cuenta o cambiar su contraseña, sus sesiones abiertas dejan de valer.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddAuthorization(options =>
    options.AddPolicy(AdminAccess.Role, policy => policy.RequireRole(AdminAccess.Role)));

// Panel de administración: páginas HTML servidas por la propia API en /admin.
// Todo /admin exige el rol Admin salvo la pantalla de inicio de sesión. Los
// formularios de Razor Pages llevan token antifalsificación automáticamente.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", AdminAccess.Role);
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
});
builder.Services.AddSingleton<AdminAccess>();

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

        // AllowCredentials: el frontend envía la cookie de sesión. Exige una
        // lista explícita de orígenes (nunca "*").
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Mensaje legible al superar un límite (antes era una página en blanco).
    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;
        response.Headers.RetryAfter = "60";
        const string message = "Demasiados intentos seguidos. Espera un minuto y vuelve a intentarlo.";

        if (context.HttpContext.Request.Path.StartsWithSegments("/admin"))
        {
            response.ContentType = "text/html; charset=utf-8";
            await response.WriteAsync(
                "<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>Demasiados intentos</title>" +
                "<link rel=\"stylesheet\" href=\"/admin/admin.css\"></head><body><main class=\"centrado\">" +
                $"<section class=\"tarjeta acceso\"><h1>Demasiados intentos</h1><p>{message}</p>" +
                "<p><a href=\"/admin/login\">Volver al inicio de sesión</a></p></section></main></body></html>",
                cancellationToken);
        }
        else
        {
            await response.WriteAsJsonAsync(
                new { title = "Demasiados intentos", status = 429, detail = message },
                cancellationToken);
        }
    };

    options.AddPolicy("reviews", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Registro y login: frena ataques de fuerza bruta por visitante (además del
    // bloqueo de cuenta tras 5 intentos fallidos). Solo cuentan los envíos
    // (POST): abrir o recargar la página de inicio de sesión no gasta intentos.
    options.AddPolicy("auth", context =>
        !HttpMethods.IsPost(context.Request.Method)
            ? RateLimitPartition.GetNoLimiter("lectura")
            : RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Latidos de presencia: la web envía uno cada 30 s; el margen cubre varias
    // pestañas o personas detrás de la misma conexión.
    options.AddPolicy("presence", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Aportaciones a la carta e importación de cafés de OSM: 30 a la hora por usuario.
    options.AddPolicy("contributions", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? ClientAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Subida de fotos: por usuario, 20 a la hora.
    options.AddPolicy("uploads", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? ClientAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromHours(1),
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

app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/health");

if (!useInMemory && !EF.IsDesignTime && app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbInitializer.SeedAsync(dbContext);
}

if (!EF.IsDesignTime)
{
    var adminAccess = app.Services.GetRequiredService<AdminAccess>();
    await adminAccess.EnsureConfiguredAdminsAsync(app.Services);

    // Garantiza que siempre haya un administrador real para entrar en /admin.
    await adminAccess.EnsureInitialAdminAsync(app.Services, app.Configuration, appDataPath, app.Logger);
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

// Dirección del visitante para los límites de peticiones. Detrás del túnel de
// Cloudflare (y del proxy de `vite preview`) todas las peticiones llegan desde
// la propia máquina; solo en ese caso se usa la IP que envía Cloudflare
// (CF-Connecting-IP) o el proxy (X-Forwarded-For). Si la petición viene de
// fuera, esas cabeceras se ignoran para que nadie pueda falsearlas.
static string ClientAddress(HttpContext context)
{
    var remote = context.Connection.RemoteIpAddress;
    if (remote is not null && System.Net.IPAddress.IsLoopback(remote))
    {
        var forwarded = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
            ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0];
        if (System.Net.IPAddress.TryParse(forwarded?.Trim(), out var client))
        {
            return client.ToString();
        }
    }

    return remote?.ToString() ?? "anonymous";
}

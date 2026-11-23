using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace LocalizadorCafes.Api.Auth;

/// <summary>
/// Da el rol de administrador a las cuentas cuyo email aparece en la
/// configuración (<c>Admin:Emails</c>). No hay emails en el código: se
/// configuran con user-secrets, una variable de entorno o appsettings local.
/// </summary>
public sealed class AdminAccess
{
    public const string Role = "Admin";

    private readonly HashSet<string> _emails;

    public AdminAccess(IConfiguration configuration)
    {
        _emails = new HashSet<string>(
            configuration.GetSection("Admin:Emails").Get<string[]>() ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool IsConfigured => _emails.Count > 0;

    public bool IsAdminEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && _emails.Contains(email.Trim());

    /// <summary>Asigna el rol si corresponde. Devuelve true si se ha añadido ahora.</summary>
    public async Task<bool> EnsureRoleAsync(
        AppUser user,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        if (!IsAdminEmail(user.Email) || await userManager.IsInRoleAsync(user, Role))
        {
            return false;
        }

        if (!await roleManager.RoleExistsAsync(Role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(Role));
        }

        await userManager.AddToRoleAsync(user, Role);
        return true;
    }

    public const string DefaultInitialEmail = "admin@localizador.local";
    public const string InitialCredentialsFile = "admin-inicial.txt";

    /// <summary>
    /// Si no hay ningún administrador, crea uno con una contraseña aleatoria y
    /// guarda las credenciales en <c>App_Data/admin-inicial.txt</c> (fuera del
    /// repositorio). Nunca se escribe la contraseña en el log. Se desactiva con
    /// <c>Admin:CreateInitialAdmin=false</c>.
    /// </summary>
    public async Task EnsureInitialAdminAsync(
        IServiceProvider services,
        IConfiguration configuration,
        string appDataPath,
        ILogger logger)
    {
        if (!configuration.GetValue("Admin:CreateInitialAdmin", true))
        {
            return;
        }

        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(Role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(Role));
        }

        if ((await userManager.GetUsersInRoleAsync(Role)).Count > 0)
        {
            return;
        }

        var email = configuration["Admin:InitialEmail"] is { Length: > 0 } configured
            ? configured.Trim()
            : DefaultInitialEmail;
        var password = GeneratePassword();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email, DisplayName = "Administrador" };
            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                logger.LogError(
                    "No se pudo crear el administrador inicial: {Errors}",
                    string.Join(" ", created.Errors.Select(error => error.Description)));
                return;
            }
        }
        else
        {
            // La cuenta existía sin rol: se le asigna una contraseña nueva conocida.
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, password);
        }

        await userManager.AddToRoleAsync(user, Role);

        Directory.CreateDirectory(appDataPath);
        var file = Path.Combine(appDataPath, InitialCredentialsFile);
        await File.WriteAllTextAsync(file,
            "Administrador inicial del Localizador de Cafés\n" +
            $"Creado: {DateTimeOffset.Now:yyyy-MM-dd HH:mm}\n\n" +
            $"Email:      {email}\n" +
            $"Contraseña: {password}\n\n" +
            "Entra en /admin, cambia la contraseña desde «Contraseña» y borra este archivo.\n");

        logger.LogWarning(
            "Se ha creado el administrador inicial {Email}. Sus credenciales están en {File}.",
            email,
            file);
    }

    /// <summary>20 caracteres sin ambigüedades (sin 0/O, 1/l/I), con al menos un dígito.</summary>
    private static string GeneratePassword()
    {
        const string letters = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        var all = letters + digits;

        var chars = Enumerable.Range(0, 19)
            .Select(_ => all[System.Security.Cryptography.RandomNumberGenerator.GetInt32(all.Length)])
            .Append(digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)])
            .ToArray();

        System.Security.Cryptography.RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }

    /// <summary>Al arrancar: da el rol a las cuentas configuradas que ya existan.</summary>
    public async Task EnsureConfiguredAdminsAsync(IServiceProvider services)
    {
        if (!IsConfigured)
        {
            return;
        }

        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var email in _emails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null)
            {
                await EnsureRoleAsync(user, userManager, roleManager);
            }
        }
    }
}

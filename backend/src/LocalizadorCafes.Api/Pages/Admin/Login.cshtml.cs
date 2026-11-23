using System.ComponentModel.DataAnnotations;
using LocalizadorCafes.Api.Auth;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace LocalizadorCafes.Api.Pages.Admin;

/// <summary>Inicio de sesión del panel: solo admite cuentas con rol Admin.</summary>
[EnableRateLimiting("auth")]
public class LoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public LoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    [Required(ErrorMessage = "Escribe tu email.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true, Name = "denegado")]
    public bool Denied { get; set; }

    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        if (User.IsInRole(AdminAccess.Role))
        {
            return LocalRedirect(SafeReturnUrl());
        }

        if (Denied)
        {
            Error = "Tu cuenta no tiene permisos de administración.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var email = Email.Trim();
        var result = await _signInManager.PasswordSignInAsync(email, Password, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            Error = "Demasiados intentos fallidos. Vuelve a intentarlo en unos minutos.";
            return Page();
        }

        if (!result.Succeeded)
        {
            Error = "El email o la contraseña no son correctos.";
            return Page();
        }

        var user = await _userManager.FindByNameAsync(email);
        if (user is null || !await _userManager.IsInRoleAsync(user, AdminAccess.Role))
        {
            // Cuenta válida pero sin rol: no se deja la sesión abierta en el panel.
            await _signInManager.SignOutAsync();
            Error = "Tu cuenta no tiene permisos de administración.";
            return Page();
        }

        return LocalRedirect(SafeReturnUrl());
    }

    /// <summary>Solo rutas locales del panel: evita redirecciones abiertas.</summary>
    private string SafeReturnUrl() =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) && ReturnUrl.StartsWith("/admin", StringComparison.Ordinal)
            ? ReturnUrl
            : "/admin";
}

using System.ComponentModel.DataAnnotations;
using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Pages.Admin;

public class ContrasenaModel : AdminPageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public ContrasenaModel(
        CafeService cafeService,
        AdminService adminService,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
        : base(cafeService, adminService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    [Required(ErrorMessage = "Escribe tu contraseña actual.")]
    [DataType(DataType.Password)]
    public string Current { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Escribe la contraseña nueva.")]
    [MinLength(8, ErrorMessage = "Usa al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string Confirm { get; set; } = string.Empty;

    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Redirect("/admin/login");
        }

        var result = await _userManager.ChangePasswordAsync(user, Current, NewPassword);
        if (!result.Succeeded)
        {
            Errors = result.Errors
                .Select(error => error.Code == "PasswordMismatch"
                    ? "La contraseña actual no es correcta."
                    : error.Description)
                .ToList();
            return Page();
        }

        // Renueva la cookie (el sello de seguridad cambia con la contraseña).
        await _signInManager.RefreshSignInAsync(user);
        await AdminService.AuditAsync(Actor, "Cambiar contraseña", "usuario", user.Id, user.Email ?? user.DisplayName);
        return Done("Contraseña cambiada. Si existe App_Data/admin-inicial.txt, bórralo.");
    }
}

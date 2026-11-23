using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LocalizadorCafes.Api.Pages.Admin;

/// <summary>Cierra la sesión (solo por POST, con token antifalsificación).</summary>
public class SalirModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;

    public SalirModel(SignInManager<AppUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public IActionResult OnGet() => Redirect("/admin");

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/admin/login");
    }
}

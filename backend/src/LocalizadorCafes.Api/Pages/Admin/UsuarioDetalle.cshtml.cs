using LocalizadorCafes.Api.Auth;
using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Pages.Admin;

public class UsuarioDetalleModel : AdminPageModel
{
    private readonly UserManager<AppUser> _userManager;

    public UsuarioDetalleModel(CafeService cafeService, AdminService adminService, UserManager<AppUser> userManager)
        : base(cafeService, adminService)
    {
        _userManager = userManager;
    }

    public AppUser? Account { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsSuspended { get; private set; }
    public IReadOnlyList<AdminReviewRow> Reviews { get; private set; } = Array.Empty<AdminReviewRow>();
    public IReadOnlyList<AdminDishRow> Dishes { get; private set; } = Array.Empty<AdminDishRow>();
    public IReadOnlyList<AdminPhotoRow> Photos { get; private set; } = Array.Empty<AdminPhotoRow>();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Account = await _userManager.FindByIdAsync(id.ToString());
        if (Account is null)
        {
            return NotFound();
        }

        IsAdmin = await _userManager.IsInRoleAsync(Account, AdminAccess.Role);
        IsSuspended = UsuariosModel.IsSuspended(Account);
        Reviews = (await AdminService.GetReviewsAsync(null, 1, userId: id, cancellationToken: cancellationToken)).Items;
        Dishes = (await AdminService.GetDishesAsync(null, null, 1, userId: id, cancellationToken: cancellationToken)).Items;
        Photos = (await AdminService.GetPhotosAsync(null, 1, userId: id, cancellationToken: cancellationToken)).Items;
        return Page();
    }
}

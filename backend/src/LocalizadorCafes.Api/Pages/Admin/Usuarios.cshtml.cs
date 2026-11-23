using LocalizadorCafes.Api.Auth;
using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Api.Pages.Admin;

public class UsuariosModel : AdminPageModel
{
    private readonly UserManager<AppUser> _userManager;

    public UsuariosModel(CafeService cafeService, AdminService adminService, UserManager<AppUser> userManager)
        : base(cafeService, adminService)
    {
        _userManager = userManager;
    }

    public record UserRow(Guid Id, string DisplayName, string Email, DateTimeOffset CreatedAt, bool IsAdmin, bool IsSuspended);

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int Pagina { get; set; } = 1;

    public PagedResult<UserRow> Result { get; private set; } = new(Array.Empty<UserRow>(), 0, 1, AdminService.PageSize);

    public Guid CurrentUserId => Actor.UserId;

    /// <summary>Bloqueo indefinido = cuenta suspendida por un administrador.</summary>
    public static bool IsSuspended(AppUser user) => user.LockoutEnd > DateTimeOffset.UtcNow.AddYears(10);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var query = _userManager.Users;
        if (!string.IsNullOrWhiteSpace(Query))
        {
            var term = Query.Trim().ToLower();
            query = query.Where(user =>
                user.DisplayName.ToLower().Contains(term) ||
                (user.Email != null && user.Email.ToLower().Contains(term)));
        }

        var page = Math.Max(1, Pagina);
        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .Skip((page - 1) * AdminService.PageSize)
            .Take(AdminService.PageSize)
            .ToListAsync(cancellationToken);
        var admins = (await _userManager.GetUsersInRoleAsync(AdminAccess.Role)).Select(user => user.Id).ToHashSet();

        Result = new PagedResult<UserRow>(
            users.Select(user => new UserRow(
                    user.Id, user.DisplayName, user.Email ?? string.Empty, user.CreatedAt,
                    admins.Contains(user.Id), IsSuspended(user)))
                .ToList(),
            total, page, AdminService.PageSize);
    }

    public async Task<IActionResult> OnPostGrantAsync(Guid id, string? volver, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Back(volver, "Esa cuenta ya no existe.");
        }

        await _userManager.AddToRoleAsync(user, AdminAccess.Role);
        await AdminService.AuditAsync(Actor, "Dar rol de administrador", "usuario", id, user.Email ?? user.DisplayName, cancellationToken);
        return Back(volver, $"{user.DisplayName} ya es administrador.");
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid id, string? volver, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Back(volver, "Esa cuenta ya no existe.");
        }

        // Nunca quitarse el rol a uno mismo ni dejar el panel sin administradores.
        if (id == CurrentUserId)
        {
            return Back(volver, "No puedes quitarte el rol de administrador a ti mismo.");
        }
        if ((await _userManager.GetUsersInRoleAsync(AdminAccess.Role)).Count <= 1)
        {
            return Back(volver, "Debe quedar al menos un administrador.");
        }

        await _userManager.RemoveFromRoleAsync(user, AdminAccess.Role);
        await _userManager.UpdateSecurityStampAsync(user);
        await AdminService.AuditAsync(Actor, "Quitar rol de administrador", "usuario", id, user.Email ?? user.DisplayName, cancellationToken);
        return Back(volver, $"{user.DisplayName} ya no es administrador.");
    }

    public async Task<IActionResult> OnPostSuspendAsync(Guid id, string? volver, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Back(volver, "Esa cuenta ya no existe.");
        }
        if (id == CurrentUserId)
        {
            return Back(volver, "No puedes suspender tu propia cuenta.");
        }
        if (await _userManager.IsInRoleAsync(user, AdminAccess.Role))
        {
            return Back(volver, "Quita primero el rol de administrador para poder suspender esta cuenta.");
        }

        // Bloqueo indefinido y sello de seguridad nuevo: no puede iniciar sesión
        // y sus sesiones abiertas caducan en menos de un minuto.
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await _userManager.UpdateSecurityStampAsync(user);
        await AdminService.AuditAsync(Actor, "Suspender cuenta", "usuario", id, user.Email ?? user.DisplayName, cancellationToken);
        return Back(volver, $"Cuenta de {user.DisplayName} suspendida: ya no puede entrar ni publicar.");
    }

    public async Task<IActionResult> OnPostReactivateAsync(Guid id, string? volver, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Back(volver, "Esa cuenta ya no existe.");
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        await AdminService.AuditAsync(Actor, "Reactivar cuenta", "usuario", id, user.Email ?? user.DisplayName, cancellationToken);
        return Back(volver, $"Cuenta de {user.DisplayName} reactivada.");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Back("/admin/usuarios", "Esa cuenta ya no existe.");
        }
        if (id == CurrentUserId)
        {
            return Back("/admin/usuarios", "No puedes borrar tu propia cuenta desde el panel.");
        }
        if (await _userManager.IsInRoleAsync(user, AdminAccess.Role))
        {
            return Back("/admin/usuarios", "Quita primero el rol de administrador para poder borrar esta cuenta.");
        }

        var label = user.Email ?? user.DisplayName;
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Back("/admin/usuarios", "No se pudo borrar la cuenta.");
        }

        // Sus reseñas, platos y fotos se conservan sin vincular a la cuenta.
        await AdminService.AuditAsync(Actor, "Borrar cuenta", "usuario", id, label, cancellationToken);
        return Back("/admin/usuarios", $"Cuenta {label} borrada. Su contenido se conserva sin vincular.");
    }
}

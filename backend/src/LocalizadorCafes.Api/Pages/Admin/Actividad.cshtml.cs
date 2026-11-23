using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Domain.Entities;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Api.Pages.Admin;

/// <summary>
/// Actividad: lo que hace la comunidad en la web (por defecto) y el registro
/// de acciones de los administradores.
/// </summary>
public class ActividadModel : AdminPageModel
{
    public const int CommunityLimit = 100;

    private readonly UserManager<AppUser> _userManager;

    public ActividadModel(CafeService cafeService, AdminService adminService, UserManager<AppUser> userManager)
        : base(cafeService, adminService)
    {
        _userManager = userManager;
    }

    public static readonly IReadOnlyList<(string? Value, string Label)> Types = new (string?, string)[]
    {
        (null, "Todo"),
        ("plato", "Platos"),
        ("foto", "Fotos"),
        ("reseña", "Reseñas"),
        ("cafetería", "Cafeterías"),
        ("usuario", "Usuarios")
    };

    /// <summary>«comunidad» (por defecto) o «admin».</summary>
    [BindProperty(SupportsGet = true, Name = "vista")]
    public string? Vista { get; set; }

    [BindProperty(SupportsGet = true, Name = "tipo")]
    public string? Tipo { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int Pagina { get; set; } = 1;

    public bool ShowAdminLog => Vista == "admin";

    public PagedResult<AdminAuditEntry> Result { get; private set; } = new(Array.Empty<AdminAuditEntry>(), 0, 1, AdminService.PageSize);

    public IReadOnlyList<CommunityEvent> Community { get; private set; } = Array.Empty<CommunityEvent>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (ShowAdminLog)
        {
            var tipo = Types.Any(type => type.Value == Tipo) ? Tipo : null;
            Result = await AdminService.GetAuditLogAsync(tipo, Pagina, cancellationToken);
            return;
        }

        var feed = (await AdminService.GetCommunityFeedAsync(CommunityLimit, cancellationToken)).ToList();

        // Cuentas nuevas (viven en Identity, no en el repositorio de cafés).
        var accounts = await _userManager.Users
            .OrderByDescending(user => user.CreatedAt)
            .Take(CommunityLimit)
            .Select(user => new { user.Id, user.DisplayName, user.Email, user.CreatedAt })
            .ToListAsync(cancellationToken);
        feed.AddRange(accounts.Select(user => new CommunityEvent(
            user.CreatedAt, CommunityEventKind.Account, user.DisplayName, user.Id,
            user.Email ?? string.Empty, null, null)));

        // Nombre del autor de los platos (el plato solo guarda su id).
        var missing = feed.Where(item => item.Who.Length == 0 && item.UserId is not null)
            .Select(item => item.UserId!.Value).Distinct().ToList();
        var names = await _userManager.Users
            .Where(user => missing.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        Community = feed
            .Select(item => item.Who.Length == 0 && item.UserId is Guid id
                ? item with { Who = names.GetValueOrDefault(id, "Cuenta eliminada") }
                : item)
            .OrderByDescending(item => item.When)
            .Take(CommunityLimit)
            .ToList();
    }

    public static string KindLabel(CommunityEventKind kind) => kind switch
    {
        CommunityEventKind.Review => "Reseña",
        CommunityEventKind.Dish => "Plato propuesto",
        CommunityEventKind.Photo => "Foto",
        CommunityEventKind.Cafe => "Cafetería guardada",
        _ => "Cuenta nueva"
    };

    /// <summary>Enlace al elemento afectado si todavía tiene página en el panel.</summary>
    public static string? TargetUrl(AdminAuditEntry entry) => entry.TargetId is Guid id
        ? entry.TargetType switch
        {
            "cafetería" when !entry.Action.StartsWith("Borrar", StringComparison.Ordinal) => $"/admin/cafes/{id}",
            "usuario" when !entry.Action.StartsWith("Borrar", StringComparison.Ordinal) => $"/admin/usuarios/{id}",
            _ => null
        }
        : null;
}

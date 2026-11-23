using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Presence;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Api.Pages.Admin;

/// <summary>Visitantes en tiempo real (con o sin cuenta).</summary>
public class EnVivoModel : AdminPageModel
{
    private readonly PresenceTracker _tracker;
    private readonly UserManager<AppUser> _userManager;

    public EnVivoModel(
        CafeService cafeService,
        AdminService adminService,
        PresenceTracker tracker,
        UserManager<AppUser> userManager)
        : base(cafeService, adminService)
    {
        _tracker = tracker;
        _userManager = userManager;
    }

    public record LiveView(PresenceSnapshot Snapshot, IReadOnlyDictionary<Guid, (string Name, string Email)> Users);

    public LiveView View { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        View = await BuildAsync(cancellationToken);

    /// <summary>Solo el bloque de datos, para el refresco automático cada pocos segundos.</summary>
    public async Task<IActionResult> OnGetFragmentoAsync(CancellationToken cancellationToken) =>
        Partial("_EnVivoContenido", await BuildAsync(cancellationToken));

    private async Task<LiveView> BuildAsync(CancellationToken cancellationToken)
    {
        var snapshot = _tracker.Snapshot();
        var ids = snapshot.Users.Select(user => user.UserId).ToList();
        var users = await _userManager.Users
            .Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.DisplayName, user.Email })
            .ToDictionaryAsync(user => user.Id, user => (user.DisplayName, user.Email ?? string.Empty), cancellationToken);

        return new LiveView(snapshot, users);
    }
}

using LocalizadorCafes.Application.Presence;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocalizadorCafes.Api.Controllers;

/// <summary>
/// «Latido» de la web para contar visitantes en tiempo real. No requiere
/// cuenta: los anónimos envían un id aleatorio de su sesión del navegador.
/// </summary>
[ApiController]
[Route("api/presence")]
public class PresenceController : ControllerBase
{
    private readonly PresenceTracker _tracker;
    private readonly UserManager<AppUser> _userManager;

    public PresenceController(PresenceTracker tracker, UserManager<AppUser> userManager)
    {
        _tracker = tracker;
        _userManager = userManager;
    }

    /// <param name="VisitorId">Id aleatorio de la sesión del navegador.</param>
    /// <param name="CafeId">Cafetería que está viendo (opcional).</param>
    /// <param name="CafeName">Nombre de esa cafetería, solo para mostrarlo en el panel.</param>
    public record HeartbeatRequest(Guid VisitorId, string? CafeId, string? CafeName);

    [HttpPost]
    [EnableRateLimiting("presence")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Beat([FromBody] HeartbeatRequest request)
    {
        if (request.VisitorId == Guid.Empty)
        {
            return BadRequest();
        }

        // Si hay sesión, el visitante se asocia a su cuenta.
        var userId = Guid.TryParse(_userManager.GetUserId(User), out var id) ? id : (Guid?)null;
        _tracker.Beat(request.VisitorId, userId, request.CafeId, request.CafeName);
        return NoContent();
    }
}

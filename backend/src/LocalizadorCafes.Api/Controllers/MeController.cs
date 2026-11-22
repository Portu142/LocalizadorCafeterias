using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Controllers;

/// <summary>Datos del usuario con sesión iniciada.</summary>
[ApiController]
[Authorize]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly CafeService _cafeService;
    private readonly UserManager<AppUser> _userManager;

    public MeController(CafeService cafeService, UserManager<AppUser> userManager)
    {
        _cafeService = cafeService;
        _userManager = userManager;
    }

    /// <summary>Reseñas del usuario, de la más reciente a la más antigua.</summary>
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(IReadOnlyList<UserReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserReviewDto>>> Reviews(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (!Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        return Ok(await _cafeService.GetUserReviewsAsync(id, cancellationToken));
    }
}

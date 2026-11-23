using LocalizadorCafes.Api.Auth;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Api.Controllers;

/// <summary>Panel de administración: revisión de platos y fotos, y reseñas.</summary>
[ApiController]
[Authorize(Roles = AdminAccess.Role)]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly CafeService _cafeService;
    private readonly UserManager<AppUser> _userManager;

    public AdminController(CafeService cafeService, UserManager<AppUser> userManager)
    {
        _cafeService = cafeService;
        _userManager = userManager;
    }

    /// <summary>Platos y fotos pendientes de revisión, del más antiguo al más reciente.</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(ModerationQueueDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModerationQueueDto>> Pending(CancellationToken cancellationToken)
    {
        var queue = await _cafeService.GetModerationQueueAsync(cancellationToken);

        // Nombre público de quien propuso cada plato.
        var userIds = queue.Dishes
            .Select(dish => dish.AddedByUserId)
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var names = await _userManager.Users
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        return Ok(queue with
        {
            Dishes = queue.Dishes
                .Select(dish => dish with
                {
                    AddedByName = dish.AddedByUserId is Guid id && names.TryGetValue(id, out var name)
                        ? name
                        : "Usuario eliminado"
                })
                .ToList()
        });
    }

    [HttpPost("dishes/{id:guid}/approve")]
    public Task<IActionResult> ApproveDish(Guid id, CancellationToken cancellationToken) =>
        Result(_cafeService.ReviewDishAsync(id, approve: true, cancellationToken));

    [HttpPost("dishes/{id:guid}/reject")]
    public Task<IActionResult> RejectDish(Guid id, CancellationToken cancellationToken) =>
        Result(_cafeService.ReviewDishAsync(id, approve: false, cancellationToken));

    [HttpPost("photos/{id:guid}/approve")]
    public Task<IActionResult> ApprovePhoto(Guid id, CancellationToken cancellationToken) =>
        Result(_cafeService.ReviewPhotoAsync(id, approve: true, cancellationToken));

    [HttpPost("photos/{id:guid}/reject")]
    public Task<IActionResult> RejectPhoto(Guid id, CancellationToken cancellationToken) =>
        Result(_cafeService.ReviewPhotoAsync(id, approve: false, cancellationToken));

    /// <summary>Últimas reseñas publicadas (se publican al momento).</summary>
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminReviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminReviewDto>>> Reviews(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _cafeService.GetRecentReviewsAsync(take, cancellationToken));

    /// <summary>Borra una reseña inapropiada.</summary>
    [HttpDelete("reviews/{id:guid}")]
    public Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken) =>
        Result(_cafeService.DeleteReviewAsync(id, cancellationToken));

    private static async Task<IActionResult> Result(Task<bool> operation) =>
        await operation ? new NoContentResult() : new NotFoundResult();
}

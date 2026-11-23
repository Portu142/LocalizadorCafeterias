using FluentValidation;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocalizadorCafes.Api.Controllers;

[ApiController]
[Route("api/cafes")]
public class CafesController : ControllerBase
{
    private readonly CafeService _cafeService;
    private readonly UserManager<AppUser> _userManager;

    public CafesController(CafeService cafeService, UserManager<AppUser> userManager)
    {
        _cafeService = cafeService;
        _userManager = userManager;
    }

    /// <summary>
    /// Busca cafeterías por texto y, opcionalmente, por cercanía usando PostGIS.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CafeSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CafeSummaryDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double? radiusKm,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (lat.HasValue != lng.HasValue)
        {
            errors["coordinates"] = new[] { "Debes indicar latitud y longitud juntas." };
        }

        if (lat is < -90 or > 90)
        {
            errors["lat"] = new[] { "La latitud debe estar entre -90 y 90." };
        }

        if (lng is < -180 or > 180)
        {
            errors["lng"] = new[] { "La longitud debe estar entre -180 y 180." };
        }

        if (radiusKm is <= 0 or > 50)
        {
            errors["radiusKm"] = new[] { "El radio debe estar entre 0 y 50 km." };
        }

        if (search is { Length: > 100 })
        {
            errors["search"] = new[] { "La búsqueda no puede superar los 100 caracteres." };
        }

        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Parámetros de búsqueda no válidos",
                Detail = "Revisa los parámetros indicados e inténtalo de nuevo.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var cafes = await _cafeService.SearchAsync(
            search,
            lat,
            lng,
            radiusKm,
            cancellationToken);

        return Ok(cafes);
    }

    /// <summary>
    /// Obtiene la ficha completa de una cafetería.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CafeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CafeDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        // Con sesión, el usuario ve también sus platos y fotos pendientes.
        var viewerId = Guid.TryParse(_userManager.GetUserId(User), out var userId) ? userId : (Guid?)null;
        var cafe = await _cafeService.GetByIdAsync(id, viewerId, cancellationToken);
        return cafe is null ? NotFound() : Ok(cafe);
    }

    /// <summary>
    /// Añade una reseña a una cafetería. Requiere sesión: el autor es el
    /// nombre de la cuenta.
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/reviews")]
    [EnableRateLimiting("reviews")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ReviewDto>> AddReview(
        Guid id,
        [FromBody] CreateReviewRequest request,
        [FromServices] IValidator<CreateReviewRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var author = await CurrentAuthorAsync();
        if (author is null)
        {
            return Unauthorized();
        }

        var review = await _cafeService.AddReviewAsync(id, request, author, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        return CreatedAtAction(nameof(GetById), new { id }, review);
    }

    /// <summary>
    /// Sube una foto de la cafetería (JPEG, PNG o WebP, máx. 5 MB). Requiere sesión.
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/photos")]
    [EnableRateLimiting("uploads")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CafePhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CafePhotoDto>> AddPhoto(
        Guid id,
        IFormFile? photo,
        CancellationToken cancellationToken)
    {
        if (photo is null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("photo", "Selecciona una foto para subir.")
            });
        }

        var author = await CurrentAuthorAsync();
        if (author is null)
        {
            return Unauthorized();
        }

        await using var stream = photo.OpenReadStream();
        var result = await _cafeService.AddPhotoAsync(id, stream, photo.Length, author, cancellationToken);

        return result is null
            ? NotFound()
            : CreatedAtAction(nameof(GetById), new { id }, result);
    }

    /// <summary>
    /// Guarda un café de OpenStreetMap en la base de datos (o devuelve el ya
    /// guardado) para poder añadirle carta, reseñas y fotos. Los datos se
    /// descargan de OSM en el servidor: el cliente solo envía el id.
    /// </summary>
    [Authorize]
    [HttpPost("import")]
    [EnableRateLimiting("contributions")]
    [ProducesResponseType(typeof(CafeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CafeDetailDto>> ImportFromOsm(
        [FromBody] ImportOsmCafeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cafe = await _cafeService.ImportOsmCafeAsync(request.OsmId, cancellationToken);

            return cafe is null
                ? Problem(
                    title: "Cafetería no encontrada",
                    detail: "Ese elemento no existe en OpenStreetMap o no es una cafetería.",
                    statusCode: StatusCodes.Status404NotFound)
                : Ok(cafe);
        }
        catch (OsmUnavailableException)
        {
            return Problem(
                title: "OpenStreetMap no responde",
                detail: "No se pudo consultar OpenStreetMap. Inténtalo de nuevo en unos segundos.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Añade un plato a la carta de una cafetería. Requiere sesión.</summary>
    [Authorize]
    [HttpPost("{id:guid}/menu")]
    [EnableRateLimiting("contributions")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemDto>> AddMenuItem(
        Guid id,
        [FromBody] CreateMenuItemRequest request,
        [FromServices] IValidator<CreateMenuItemRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var author = await CurrentAuthorAsync();
        if (author is null)
        {
            return Unauthorized();
        }

        var item = await _cafeService.AddMenuItemAsync(id, request, author, cancellationToken);
        return item is null ? NotFound() : CreatedAtAction(nameof(GetById), new { id }, item);
    }

    /// <summary>
    /// Borra un plato añadido por el propio usuario, si nadie más lo ha puntuado.
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}/menu/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMenuItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var author = await CurrentAuthorAsync();
        if (author is null)
        {
            return Unauthorized();
        }

        return await _cafeService.DeleteMenuItemAsync(id, itemId, author.UserId, cancellationToken) switch
        {
            CafeService.DeleteMenuItemResult.Deleted => NoContent(),
            CafeService.DeleteMenuItemResult.Forbidden => Problem(
                title: "No puedes borrar este plato",
                detail: "Solo quien añadió un plato puede borrarlo.",
                statusCode: StatusCodes.Status403Forbidden),
            CafeService.DeleteMenuItemResult.AlreadyRated => Problem(
                title: "El plato ya tiene puntuaciones",
                detail: "Otros usuarios ya lo han puntuado, así que no se puede borrar.",
                statusCode: StatusCodes.Status409Conflict),
            _ => NotFound()
        };
    }

    private async Task<ReviewAuthor?> CurrentAuthorAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        // Una cuenta bloqueada desde el panel no puede publicar nada.
        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        return new ReviewAuthor(user.Id, user.DisplayName);
    }
}

using FluentValidation;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocalizadorCafes.Api.Controllers;

[ApiController]
[Route("api/cafes")]
public class CafesController : ControllerBase
{
    private readonly CafeService _cafeService;

    public CafesController(CafeService cafeService)
    {
        _cafeService = cafeService;
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
        var cafe = await _cafeService.GetByIdAsync(id, cancellationToken);
        return cafe is null ? NotFound() : Ok(cafe);
    }

    /// <summary>
    /// Añade una reseña a una cafetería.
    /// </summary>
    [HttpPost("{id:guid}/reviews")]
    [EnableRateLimiting("reviews")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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

        var review = await _cafeService.AddReviewAsync(id, request, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        return CreatedAtAction(nameof(GetById), new { id }, review);
    }
}

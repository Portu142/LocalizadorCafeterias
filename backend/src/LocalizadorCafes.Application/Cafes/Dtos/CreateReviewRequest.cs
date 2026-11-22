namespace LocalizadorCafes.Application.Cafes.Dtos;

/// <summary>
/// Datos de una reseña nueva. El autor ya no lo envía el cliente: se toma de
/// la cuenta con la que se ha iniciado sesión. Se conserva el campo por
/// compatibilidad con clientes antiguos, pero se ignora.
/// </summary>
public record CreateReviewRequest(
    string? Author,
    int Rating,
    string Comment,
    IReadOnlyList<CreateMenuItemRating>? ItemRatings = null);

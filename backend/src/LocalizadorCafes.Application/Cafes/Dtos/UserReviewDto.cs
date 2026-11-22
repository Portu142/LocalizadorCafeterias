namespace LocalizadorCafes.Application.Cafes.Dtos;

/// <summary>Reseña propia con la cafetería a la que pertenece («Mis reseñas»).</summary>
public record UserReviewDto(
    Guid Id,
    Guid CafeId,
    string CafeName,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);

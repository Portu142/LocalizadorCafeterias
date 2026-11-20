namespace LocalizadorCafes.Application.Cafes.Dtos;

public record ReviewDto(
    Guid Id,
    string Author,
    int Rating,
    int MenuRating,
    string Comment,
    DateTimeOffset CreatedAt);

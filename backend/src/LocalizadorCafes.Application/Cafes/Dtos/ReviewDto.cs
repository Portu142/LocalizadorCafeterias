namespace LocalizadorCafes.Application.Cafes.Dtos;

public record ReviewDto(
    Guid Id,
    string Author,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt,
    IReadOnlyList<MenuItemRatingDto> ItemRatings);

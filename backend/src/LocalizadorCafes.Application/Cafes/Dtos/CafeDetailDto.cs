namespace LocalizadorCafes.Application.Cafes.Dtos;

public record CafeDetailDto(
    Guid Id,
    string Name,
    string Address,
    string Neighborhood,
    string Phone,
    string Hours,
    double Rating,
    int ReviewCount,
    double Latitude,
    double Longitude,
    IReadOnlyList<string> Tags,
    IReadOnlyList<CafePhotoDto> Photos,
    IReadOnlyList<MenuItemDto> Menu,
    IReadOnlyList<ReviewDto> Reviews);

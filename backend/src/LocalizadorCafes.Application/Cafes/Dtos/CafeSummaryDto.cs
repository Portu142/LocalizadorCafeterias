namespace LocalizadorCafes.Application.Cafes.Dtos;

public record CafeSummaryDto(
    Guid Id,
    string Name,
    string Address,
    string Neighborhood,
    double Rating,
    int ReviewCount,
    double Latitude,
    double Longitude);

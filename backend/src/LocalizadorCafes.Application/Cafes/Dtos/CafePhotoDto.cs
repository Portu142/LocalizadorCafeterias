namespace LocalizadorCafes.Application.Cafes.Dtos;

public record CafePhotoDto(
    Guid Id,
    string Url,
    string Author,
    string License,
    string SourceUrl);

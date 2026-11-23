namespace LocalizadorCafes.Application.Cafes.Dtos;

/// <param name="Status">«approved» o «pending» (este solo lo ve quien la subió).</param>
public record CafePhotoDto(
    Guid Id,
    string Url,
    string Author,
    string License,
    string SourceUrl,
    string Status = "approved");

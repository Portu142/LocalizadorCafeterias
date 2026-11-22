namespace LocalizadorCafes.Application.Cafes.Dtos;

/// <summary>Usuario autenticado que publica una reseña o sube una foto.</summary>
public record ReviewAuthor(Guid UserId, string DisplayName);

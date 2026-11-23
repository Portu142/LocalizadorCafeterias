namespace LocalizadorCafes.Application.Cafes.Dtos;

/// <summary>Plato pendiente de revisión en el panel de administración.</summary>
public record PendingDishDto(
    Guid Id,
    Guid CafeId,
    string CafeName,
    string Name,
    decimal Price,
    Guid? AddedByUserId,
    string AddedByName,
    DateTimeOffset CreatedAt);

/// <summary>Foto pendiente de revisión en el panel de administración.</summary>
public record PendingPhotoDto(
    Guid Id,
    Guid CafeId,
    string CafeName,
    string Url,
    string Author,
    DateTimeOffset CreatedAt);

public record ModerationQueueDto(
    IReadOnlyList<PendingDishDto> Dishes,
    IReadOnlyList<PendingPhotoDto> Photos);

/// <summary>Reseña reciente (para poder borrar las inapropiadas).</summary>
public record AdminReviewDto(
    Guid Id,
    Guid CafeId,
    string CafeName,
    string Author,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);

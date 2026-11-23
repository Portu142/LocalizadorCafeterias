using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Application.Common.Interfaces;

public interface ICafeRepository
{
    Task<IReadOnlyList<Cafe>> SearchAsync(
        string? search,
        double? latitude,
        double? longitude,
        double? radiusKm,
        CancellationToken cancellationToken = default);

    Task<Cafe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddReviewAsync(Review review, CancellationToken cancellationToken = default);

    Task AddPhotoAsync(CafePhoto photo, CancellationToken cancellationToken = default);

    Task<Cafe?> GetByOsmIdAsync(string osmId, CancellationToken cancellationToken = default);

    Task AddCafeAsync(Cafe cafe, CancellationToken cancellationToken = default);

    Task AddMenuItemAsync(MenuItem item, CancellationToken cancellationToken = default);

    Task RemoveMenuItemAsync(MenuItem item, CancellationToken cancellationToken = default);

    /// <summary>Platos y fotos pendientes de revisión, con su cafetería.</summary>
    Task<IReadOnlyList<MenuItem>> GetPendingMenuItemsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CafePhoto>> GetPendingPhotosAsync(CancellationToken cancellationToken = default);

    /// <summary>Cambia el estado; false si no existe.</summary>
    Task<bool> SetMenuItemStatusAsync(Guid id, ModerationStatus status, CancellationToken cancellationToken = default);

    Task<bool> SetPhotoStatusAsync(Guid id, ModerationStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Review Review, Cafe Cafe)>> GetRecentReviewsAsync(int take, CancellationToken cancellationToken = default);

    Task<bool> DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Reseñas de un usuario, de la más reciente a la más antigua, con su cafetería.</summary>
    Task<IReadOnlyList<(Review Review, Cafe Cafe)>> GetReviewsByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

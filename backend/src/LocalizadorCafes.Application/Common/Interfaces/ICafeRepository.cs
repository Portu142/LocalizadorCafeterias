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

    /// <summary>Reseñas de un usuario, de la más reciente a la más antigua, con su cafetería.</summary>
    Task<IReadOnlyList<(Review Review, Cafe Cafe)>> GetReviewsByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

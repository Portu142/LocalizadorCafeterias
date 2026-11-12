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
}

using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio en memoria que permite arrancar la API sin PostgreSQL (modo
/// demo). Las reseñas creadas se pierden al reiniciar el proceso.
/// </summary>
public sealed class InMemoryCafeRepository : ICafeRepository
{
    private const double EarthRadiusKm = 6371;
    private readonly object _sync = new();
    private readonly List<Cafe> _cafes;

    public InMemoryCafeRepository(IEnumerable<Cafe> cafes)
    {
        _cafes = cafes.ToList();
    }

    public Task<IReadOnlyList<Cafe>> SearchAsync(
        string? search,
        double? latitude,
        double? longitude,
        double? radiusKm,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IEnumerable<Cafe> query = _cafes;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(cafe =>
                    Contains(cafe.Name, term) ||
                    Contains(cafe.Address, term) ||
                    Contains(cafe.Neighborhood, term));
            }

            if (latitude.HasValue && longitude.HasValue)
            {
                var radius = radiusKm ?? 5;
                query = query.Where(cafe =>
                    DistanceKm(latitude.Value, longitude.Value, cafe.Location.Y, cafe.Location.X) <= radius);
            }

            IReadOnlyList<Cafe> result = query
                .OrderByDescending(cafe => cafe.Reviews.Count == 0
                    ? 0
                    : cafe.Reviews.Average(review => review.Rating))
                .ThenBy(cafe => cafe.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult(result);
        }
    }

    public Task<Cafe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_cafes.FirstOrDefault(cafe => cafe.Id == id));
        }
    }

    public Task AddReviewAsync(Review review, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var cafe = _cafes.FirstOrDefault(item => item.Id == review.CafeId);
            if (cafe is not null)
            {
                review.Cafe = cafe;
                cafe.Reviews.Add(review);
            }

            return Task.CompletedTask;
        }
    }

    public Task AddPhotoAsync(CafePhoto photo, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var cafe = _cafes.FirstOrDefault(item => item.Id == photo.CafeId);
            if (cafe is not null)
            {
                photo.Cafe = cafe;
                cafe.Photos.Add(photo);
            }

            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<(Review Review, Cafe Cafe)>> GetReviewsByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<(Review Review, Cafe Cafe)> result = _cafes
                .SelectMany(cafe => cafe.Reviews
                    .Where(review => review.UserId == userId)
                    .Select(review => (review, cafe)))
                .OrderByDescending(item => item.review.CreatedAt)
                .ToList();

            return Task.FromResult(result);
        }
    }

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static double DistanceKm(double fromLat, double fromLng, double toLat, double toLng)
    {
        var dLat = ToRadians(toLat - fromLat);
        var dLng = ToRadians(toLng - fromLng);
        var lat1 = ToRadians(fromLat);
        var lat2 = ToRadians(toLat);

        var haversine = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(haversine));
    }

    private static double ToRadians(double value) => value * Math.PI / 180;
}

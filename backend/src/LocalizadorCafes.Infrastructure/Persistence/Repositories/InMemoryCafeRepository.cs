using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio en memoria que permite arrancar la API sin PostgreSQL (modo
/// demo). Las reseñas creadas se pierden al reiniciar el proceso.
/// </summary>
public sealed partial class InMemoryCafeRepository : ICafeRepository, IAdminRepository
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

    public Task<Cafe?> GetByOsmIdAsync(string osmId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_cafes.FirstOrDefault(cafe => cafe.OsmId == osmId));
        }
    }

    public Task AddCafeAsync(Cafe cafe, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _cafes.Add(cafe);
            return Task.CompletedTask;
        }
    }

    public Task AddMenuItemAsync(MenuItem item, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var cafe = _cafes.FirstOrDefault(entry => entry.Id == item.CafeId);
            if (cafe is not null)
            {
                item.Cafe = cafe;
                cafe.Menu.Add(item);
            }

            return Task.CompletedTask;
        }
    }

    public Task RemoveMenuItemAsync(MenuItem item, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var cafe = _cafes.FirstOrDefault(entry => entry.Id == item.CafeId);
            if (cafe is not null)
            {
                cafe.Menu.Remove(item);
                foreach (var review in cafe.Reviews)
                {
                    foreach (var rating in review.MenuItemRatings.Where(rating => rating.MenuItemId == item.Id).ToList())
                    {
                        review.MenuItemRatings.Remove(rating);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<MenuItem>> GetPendingMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<MenuItem> result = _cafes
                .SelectMany(cafe => cafe.Menu.Where(item => item.Status == ModerationStatus.Pending)
                    .Select(item => { item.Cafe = cafe; return item; }))
                .OrderBy(item => item.CreatedAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<CafePhoto>> GetPendingPhotosAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<CafePhoto> result = _cafes
                .SelectMany(cafe => cafe.Photos.Where(photo => photo.Status == ModerationStatus.Pending)
                    .Select(photo => { photo.Cafe = cafe; return photo; }))
                .OrderBy(photo => photo.CreatedAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<bool> SetMenuItemStatusAsync(Guid id, ModerationStatus status, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var item = _cafes.SelectMany(cafe => cafe.Menu).FirstOrDefault(entry => entry.Id == id);
            if (item is not null)
            {
                item.Status = status;
            }
            return Task.FromResult(item is not null);
        }
    }

    public Task<bool> SetPhotoStatusAsync(Guid id, ModerationStatus status, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var photo = _cafes.SelectMany(cafe => cafe.Photos).FirstOrDefault(entry => entry.Id == id);
            if (photo is not null)
            {
                photo.Status = status;
            }
            return Task.FromResult(photo is not null);
        }
    }

    public Task<IReadOnlyList<(Review Review, Cafe Cafe)>> GetRecentReviewsAsync(int take, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<(Review Review, Cafe Cafe)> result = _cafes
                .SelectMany(cafe => cafe.Reviews.Select(review => (review, cafe)))
                .OrderByDescending(item => item.review.CreatedAt)
                .Take(take)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<bool> DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            foreach (var cafe in _cafes)
            {
                var review = cafe.Reviews.FirstOrDefault(entry => entry.Id == id);
                if (review is not null)
                {
                    cafe.Reviews.Remove(review);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
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

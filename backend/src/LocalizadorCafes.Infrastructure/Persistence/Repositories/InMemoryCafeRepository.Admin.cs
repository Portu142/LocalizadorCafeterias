using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Domain.Entities;

namespace LocalizadorCafes.Infrastructure.Persistence.Repositories;

/// <summary>Consultas del panel de administración en el modo demo (en memoria).</summary>
public sealed partial class InMemoryCafeRepository
{
    private static bool Matches(string value, string? term) =>
        term is null || value.Contains(term, StringComparison.OrdinalIgnoreCase);

    public Task<(IReadOnlyList<AdminCafeRow> Items, int Total)> SearchCafesAsync(
        string? term, int skip, int take, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var matches = _cafes
                .Where(cafe => Matches(cafe.Name, term) || Matches(cafe.Address, term) || Matches(cafe.Neighborhood, term))
                .OrderBy(cafe => cafe.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            IReadOnlyList<AdminCafeRow> page = matches
                .Skip(skip)
                .Take(take)
                .Select(cafe => new AdminCafeRow(
                    cafe.Id, cafe.Name, cafe.Address, cafe.Neighborhood, cafe.OsmId,
                    cafe.Menu.Count(item => item.Status == ModerationStatus.Approved),
                    cafe.Menu.Count(item => item.Status == ModerationStatus.Pending) +
                        cafe.Photos.Count(photo => photo.Status == ModerationStatus.Pending),
                    cafe.Photos.Count(photo => photo.Status == ModerationStatus.Approved),
                    cafe.Reviews.Count,
                    cafe.CreatedAt))
                .ToList();

            return Task.FromResult((page, matches.Count));
        }
    }

    public Task<bool> UpdateCafeAsync(Guid id, CafeEdit edit, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var cafe = _cafes.FirstOrDefault(entry => entry.Id == id);
            if (cafe is null)
            {
                return Task.FromResult(false);
            }

            cafe.Name = edit.Name;
            cafe.Address = edit.Address;
            cafe.Neighborhood = edit.Neighborhood;
            cafe.Phone = edit.Phone;
            cafe.Hours = edit.Hours;
            cafe.Tags = edit.Tags.ToList();
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteCafeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_cafes.RemoveAll(cafe => cafe.Id == id) > 0);
        }
    }

    public Task<(IReadOnlyList<AdminDishRow> Items, int Total)> GetDishesAsync(
        ModerationStatus? status, string? term, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var matches = _cafes
                .Where(cafe => cafeId is null || cafe.Id == cafeId)
                .SelectMany(cafe => cafe.Menu.Select(item => (cafe, item)))
                .Where(entry => status is null || entry.item.Status == status)
                .Where(entry => userId is null || entry.item.AddedByUserId == userId)
                .Where(entry => Matches(entry.item.Name, term) || Matches(entry.cafe.Name, term))
                .OrderByDescending(entry => entry.item.CreatedAt)
                .ToList();

            IReadOnlyList<AdminDishRow> page = matches
                .Skip(skip)
                .Take(take)
                .Select(entry => new AdminDishRow(
                    entry.item.Id, entry.cafe.Id, entry.cafe.Name, entry.item.Name, entry.item.Price,
                    entry.item.Status, entry.item.AddedByUserId, entry.item.CreatedAt))
                .ToList();

            return Task.FromResult((page, matches.Count));
        }
    }

    public Task<bool> UpdateDishAsync(Guid id, string name, decimal price, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var item = _cafes.SelectMany(cafe => cafe.Menu).FirstOrDefault(entry => entry.Id == id);
            if (item is null)
            {
                return Task.FromResult(false);
            }

            item.Name = name;
            item.Price = price;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteDishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            foreach (var cafe in _cafes)
            {
                var item = cafe.Menu.FirstOrDefault(entry => entry.Id == id);
                if (item is null)
                {
                    continue;
                }

                cafe.Menu.Remove(item);
                foreach (var review in cafe.Reviews)
                {
                    foreach (var rating in review.MenuItemRatings.Where(rating => rating.MenuItemId == id).ToList())
                    {
                        review.MenuItemRatings.Remove(rating);
                    }
                }
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }

    public Task<(IReadOnlyList<AdminPhotoRow> Items, int Total)> GetPhotosAsync(
        ModerationStatus? status, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var matches = _cafes
                .Where(cafe => cafeId is null || cafe.Id == cafeId)
                .SelectMany(cafe => cafe.Photos.Select(photo => (cafe, photo)))
                .Where(entry => status is null || entry.photo.Status == status)
                .Where(entry => userId is null || entry.photo.UploadedByUserId == userId)
                .OrderByDescending(entry => entry.photo.CreatedAt)
                .ToList();

            IReadOnlyList<AdminPhotoRow> page = matches
                .Skip(skip)
                .Take(take)
                .Select(entry => new AdminPhotoRow(
                    entry.photo.Id, entry.cafe.Id, entry.cafe.Name, entry.photo.Url, entry.photo.Author,
                    entry.photo.License, entry.photo.Status, entry.photo.UploadedByUserId, entry.photo.CreatedAt))
                .ToList();

            return Task.FromResult((page, matches.Count));
        }
    }

    public Task<CafePhoto?> DeletePhotoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            foreach (var cafe in _cafes)
            {
                var photo = cafe.Photos.FirstOrDefault(entry => entry.Id == id);
                if (photo is not null)
                {
                    cafe.Photos.Remove(photo);
                    return Task.FromResult<CafePhoto?>(photo);
                }
            }

            return Task.FromResult<CafePhoto?>(null);
        }
    }

    public Task<(IReadOnlyList<AdminReviewRow> Items, int Total)> GetReviewsAsync(
        string? term, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var matches = _cafes
                .Where(cafe => cafeId is null || cafe.Id == cafeId)
                .SelectMany(cafe => cafe.Reviews.Select(review => (cafe, review)))
                .Where(entry => userId is null || entry.review.UserId == userId)
                .Where(entry => Matches(entry.review.Comment, term) || Matches(entry.review.Author, term) || Matches(entry.cafe.Name, term))
                .OrderByDescending(entry => entry.review.CreatedAt)
                .ToList();

            IReadOnlyList<AdminReviewRow> page = matches
                .Skip(skip)
                .Take(take)
                .Select(entry => new AdminReviewRow(
                    entry.review.Id, entry.cafe.Id, entry.cafe.Name, entry.review.UserId,
                    entry.review.Author, entry.review.Rating, entry.review.Comment, entry.review.CreatedAt))
                .ToList();

            return Task.FromResult((page, matches.Count));
        }
    }

    public Task<ActivityDates> GetActivityDatesAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(new ActivityDates(
                _cafes.SelectMany(cafe => cafe.Reviews).Select(review => review.CreatedAt).Where(date => date >= since).ToList(),
                _cafes.SelectMany(cafe => cafe.Menu).Where(item => item.AddedByUserId != null).Select(item => item.CreatedAt).Where(date => date >= since).ToList(),
                _cafes.SelectMany(cafe => cafe.Photos).Where(photo => photo.UploadedByUserId != null).Select(photo => photo.CreatedAt).Where(date => date >= since).ToList()));
        }
    }
}

using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.Infrastructure.Persistence.Repositories;

public class CafeRepository : ICafeRepository
{
    private readonly AppDbContext _dbContext;

    public CafeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Cafe>> SearchAsync(
        string? search,
        double? latitude,
        double? longitude,
        double? radiusKm,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Cafes
            .AsNoTracking()
            .Include(cafe => cafe.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(cafe =>
                cafe.Name.ToLower().Contains(term) ||
                cafe.Address.ToLower().Contains(term) ||
                cafe.Neighborhood.ToLower().Contains(term));
        }

        if (latitude.HasValue && longitude.HasValue)
        {
            var point = new Point(longitude.Value, latitude.Value) { SRID = 4326 };
            var radiusMeters = (radiusKm ?? 5) * 1000;
            query = query.Where(cafe => cafe.Location.Distance(point) <= radiusMeters);
        }

        return await query
            .OrderByDescending(cafe =>
                cafe.Reviews.Average(review => (double?)review.Rating) ?? 0)
            .ThenBy(cafe => cafe.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Cafe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Cafes
            .AsNoTracking()
            // Tres colecciones (menú, fotos y reseñas): consultas separadas evitan
            // el producto cartesiano de filas de un único JOIN.
            .AsSplitQuery()
            .Include(cafe => cafe.Menu)
            .Include(cafe => cafe.Photos)
            .Include(cafe => cafe.Reviews)
                .ThenInclude(review => review.MenuItemRatings)
            .FirstOrDefaultAsync(cafe => cafe.Id == id, cancellationToken);
    }

    public async Task AddReviewAsync(Review review, CancellationToken cancellationToken = default)
    {
        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPhotoAsync(CafePhoto photo, CancellationToken cancellationToken = default)
    {
        _dbContext.CafePhotos.Add(photo);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Cafe?> GetByOsmIdAsync(string osmId, CancellationToken cancellationToken = default)
    {
        var id = await _dbContext.Cafes
            .Where(cafe => cafe.OsmId == osmId)
            .Select(cafe => (Guid?)cafe.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id is null ? null : await GetByIdAsync(id.Value, cancellationToken);
    }

    public async Task AddCafeAsync(Cafe cafe, CancellationToken cancellationToken = default)
    {
        _dbContext.Cafes.Add(cafe);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMenuItemAsync(MenuItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.MenuItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMenuItemAsync(MenuItem item, CancellationToken cancellationToken = default)
    {
        await _dbContext.MenuItems
            .Where(menuItem => menuItem.Id == item.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItem>> GetPendingMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Include(item => item.Cafe)
            .Where(item => item.Status == ModerationStatus.Pending)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CafePhoto>> GetPendingPhotosAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CafePhotos
            .AsNoTracking()
            .Include(photo => photo.Cafe)
            .Where(photo => photo.Status == ModerationStatus.Pending)
            .OrderBy(photo => photo.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SetMenuItemStatusAsync(Guid id, ModerationStatus status, CancellationToken cancellationToken = default)
    {
        var updated = await _dbContext.MenuItems
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, status)
                .SetProperty(item => item.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
        return updated > 0;
    }

    public async Task<bool> SetPhotoStatusAsync(Guid id, ModerationStatus status, CancellationToken cancellationToken = default)
    {
        var updated = await _dbContext.CafePhotos
            .Where(photo => photo.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(photo => photo.Status, status)
                .SetProperty(photo => photo.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
        return updated > 0;
    }

    public async Task<IReadOnlyList<(Review Review, Cafe Cafe)>> GetRecentReviewsAsync(int take, CancellationToken cancellationToken = default)
    {
        var reviews = await _dbContext.Reviews
            .AsNoTracking()
            .Include(review => review.Cafe)
            .OrderByDescending(review => review.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return reviews.Select(review => (review, review.Cafe)).ToList();
    }

    public async Task<bool> DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _dbContext.Reviews
            .Where(review => review.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    public async Task<IReadOnlyList<(Review Review, Cafe Cafe)>> GetReviewsByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _dbContext.Reviews
            .AsNoTracking()
            .Include(review => review.Cafe)
            .Where(review => review.UserId == userId)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync(cancellationToken);

        return reviews.Select(review => (review, review.Cafe)).ToList();
    }
}

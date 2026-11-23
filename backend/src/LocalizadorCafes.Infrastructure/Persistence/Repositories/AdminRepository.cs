using LocalizadorCafes.Application.Admin;
using LocalizadorCafes.Application.Common.Interfaces;
using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Infrastructure.Persistence.Repositories;

/// <summary>Consultas del panel de administración sobre PostgreSQL.</summary>
public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _dbContext;

    public AdminRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Búsqueda sin distinguir mayúsculas (ILIKE de PostgreSQL).</summary>
    private static string Pattern(string term) =>
        "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    public async Task<(IReadOnlyList<AdminCafeRow> Items, int Total)> SearchCafesAsync(
        string? term, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Cafes.AsNoTracking();

        if (term is not null)
        {
            var pattern = Pattern(term);
            query = query.Where(cafe =>
                EF.Functions.ILike(cafe.Name, pattern) ||
                EF.Functions.ILike(cafe.Address, pattern) ||
                EF.Functions.ILike(cafe.Neighborhood, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(cafe => cafe.Name)
            .Skip(skip)
            .Take(take)
            .Select(cafe => new AdminCafeRow(
                cafe.Id,
                cafe.Name,
                cafe.Address,
                cafe.Neighborhood,
                cafe.OsmId,
                cafe.Menu.Count(item => item.Status == ModerationStatus.Approved),
                cafe.Menu.Count(item => item.Status == ModerationStatus.Pending) +
                    cafe.Photos.Count(photo => photo.Status == ModerationStatus.Pending),
                cafe.Photos.Count(photo => photo.Status == ModerationStatus.Approved),
                cafe.Reviews.Count,
                cafe.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> UpdateCafeAsync(Guid id, CafeEdit edit, CancellationToken cancellationToken = default)
    {
        var cafe = await _dbContext.Cafes.FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);
        if (cafe is null)
        {
            return false;
        }

        cafe.Name = edit.Name;
        cafe.Address = edit.Address;
        cafe.Neighborhood = edit.Neighborhood;
        cafe.Phone = edit.Phone;
        cafe.Hours = edit.Hours;
        cafe.Tags = edit.Tags.ToList();
        cafe.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCafeAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.Cafes.Where(cafe => cafe.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<(IReadOnlyList<AdminDishRow> Items, int Total)> GetDishesAsync(
        ModerationStatus? status, string? term, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MenuItems.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(item => item.Status == status);
        }
        if (userId is not null)
        {
            query = query.Where(item => item.AddedByUserId == userId);
        }
        if (cafeId is not null)
        {
            query = query.Where(item => item.CafeId == cafeId);
        }
        if (term is not null)
        {
            var pattern = Pattern(term);
            query = query.Where(item => EF.Functions.ILike(item.Name, pattern) || EF.Functions.ILike(item.Cafe.Name, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(item => new AdminDishRow(
                item.Id, item.CafeId, item.Cafe.Name, item.Name, item.Price,
                item.Status, item.AddedByUserId, item.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> UpdateDishAsync(Guid id, string name, decimal price, CancellationToken cancellationToken = default) =>
        await _dbContext.MenuItems
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, name)
                .SetProperty(item => item.Price, price)
                .SetProperty(item => item.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken) > 0;

    public async Task<bool> DeleteDishAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.MenuItems.Where(item => item.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<(IReadOnlyList<AdminPhotoRow> Items, int Total)> GetPhotosAsync(
        ModerationStatus? status, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CafePhotos.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(photo => photo.Status == status);
        }
        if (userId is not null)
        {
            query = query.Where(photo => photo.UploadedByUserId == userId);
        }
        if (cafeId is not null)
        {
            query = query.Where(photo => photo.CafeId == cafeId);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(photo => photo.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(photo => new AdminPhotoRow(
                photo.Id, photo.CafeId, photo.Cafe.Name, photo.Url, photo.Author, photo.License,
                photo.Status, photo.UploadedByUserId, photo.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<CafePhoto?> DeletePhotoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var photo = await _dbContext.CafePhotos.FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);
        if (photo is null)
        {
            return null;
        }

        _dbContext.CafePhotos.Remove(photo);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return photo;
    }

    public async Task<(IReadOnlyList<AdminReviewRow> Items, int Total)> GetReviewsAsync(
        string? term, Guid? userId, Guid? cafeId,
        int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Reviews.AsNoTracking();

        if (userId is not null)
        {
            query = query.Where(review => review.UserId == userId);
        }
        if (cafeId is not null)
        {
            query = query.Where(review => review.CafeId == cafeId);
        }
        if (term is not null)
        {
            var pattern = Pattern(term);
            query = query.Where(review =>
                EF.Functions.ILike(review.Comment, pattern) ||
                EF.Functions.ILike(review.Author, pattern) ||
                EF.Functions.ILike(review.Cafe.Name, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(review => review.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(review => new AdminReviewRow(
                review.Id, review.CafeId, review.Cafe.Name, review.UserId,
                review.Author, review.Rating, review.Comment, review.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<ActivityDates> GetActivityDatesAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        var reviews = await _dbContext.Reviews
            .Where(review => review.CreatedAt >= since)
            .Select(review => review.CreatedAt)
            .ToListAsync(cancellationToken);
        var dishes = await _dbContext.MenuItems
            .Where(item => item.AddedByUserId != null && item.CreatedAt >= since)
            .Select(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var photos = await _dbContext.CafePhotos
            .Where(photo => photo.UploadedByUserId != null && photo.CreatedAt >= since)
            .Select(photo => photo.CreatedAt)
            .ToListAsync(cancellationToken);

        return new ActivityDates(reviews, dishes, photos);
    }
}

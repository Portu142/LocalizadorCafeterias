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
            .Include(cafe => cafe.Menu)
            .Include(cafe => cafe.Reviews)
            .FirstOrDefaultAsync(cafe => cafe.Id == id, cancellationToken);
    }

    public async Task AddReviewAsync(Review review, CancellationToken cancellationToken = default)
    {
        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

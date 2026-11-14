using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Cafes.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Cafes.AddRange(DemoData.CreateCafes());
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

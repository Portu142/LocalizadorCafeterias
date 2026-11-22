using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Cafes.AnyAsync(cancellationToken))
        {
            dbContext.Cafes.AddRange(DemoData.CreateCafes());
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await BackfillTagsAndPhotosAsync(dbContext, cancellationToken);
    }

    /// <summary>
    /// Las bases de datos sembradas antes de existir etiquetas y fotos se
    /// completan con los datos de demo, emparejando por nombre. Solo rellena lo
    /// que está vacío: nunca pisa datos editados a mano.
    /// </summary>
    private static async Task BackfillTagsAndPhotosAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var demoByName = DemoData.CreateCafes().ToDictionary(cafe => cafe.Name);
        var names = demoByName.Keys.ToList();

        var cafes = await dbContext.Cafes
            .Include(cafe => cafe.Photos)
            .Where(cafe => names.Contains(cafe.Name))
            .ToListAsync(cancellationToken);

        var changed = false;

        foreach (var cafe in cafes)
        {
            var demo = demoByName[cafe.Name];

            if (cafe.Tags.Count == 0 && demo.Tags.Count > 0)
            {
                cafe.Tags = demo.Tags.ToList();
                changed = true;
            }

            if (cafe.Photos.Count == 0 && demo.Photos.Count > 0)
            {
                foreach (var photo in demo.Photos)
                {
                    dbContext.CafePhotos.Add(new CafePhoto
                    {
                        CafeId = cafe.Id,
                        Url = photo.Url,
                        Author = photo.Author,
                        License = photo.License,
                        SourceUrl = photo.SourceUrl,
                        CreatedAt = photo.CreatedAt
                    });
                }

                changed = true;
            }
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

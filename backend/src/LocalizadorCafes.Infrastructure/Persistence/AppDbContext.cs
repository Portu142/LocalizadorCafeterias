using LocalizadorCafes.Domain.Entities;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cafe> Cafes => Set<Cafe>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<MenuItemRating> MenuItemRatings => Set<MenuItemRating>();
    public DbSet<CafePhoto> CafePhotos => Set<CafePhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Primero el modelo de Identity (tablas AspNet*) y después el nuestro.
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<AppUser>(user =>
        {
            user.Property(item => item.DisplayName)
                .HasMaxLength(40)
                .IsRequired();
        });
    }
}

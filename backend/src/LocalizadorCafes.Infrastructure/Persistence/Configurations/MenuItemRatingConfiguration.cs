using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalizadorCafes.Infrastructure.Persistence.Configurations;

public class MenuItemRatingConfiguration : IEntityTypeConfiguration<MenuItemRating>
{
    public void Configure(EntityTypeBuilder<MenuItemRating> builder)
    {
        builder.ToTable("menu_item_ratings", table =>
            table.HasCheckConstraint("CK_menu_item_ratings_score", "\"Score\" BETWEEN 1 AND 5"));

        builder.HasKey(rating => rating.Id);

        builder.Property(rating => rating.Score)
            .IsRequired();

        // Una reseña solo puede puntuar cada plato una vez.
        builder.HasIndex(rating => new { rating.ReviewId, rating.MenuItemId })
            .IsUnique();

        builder.HasOne(rating => rating.Review)
            .WithMany(review => review.MenuItemRatings)
            .HasForeignKey(rating => rating.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rating => rating.MenuItem)
            .WithMany(item => item.Ratings)
            .HasForeignKey(rating => rating.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

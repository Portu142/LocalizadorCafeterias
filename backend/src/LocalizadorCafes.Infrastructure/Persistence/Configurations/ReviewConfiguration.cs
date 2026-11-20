using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalizadorCafes.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", table =>
        {
            table.HasCheckConstraint("CK_reviews_rating", "\"Rating\" BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_reviews_menu_rating", "\"MenuRating\" BETWEEN 1 AND 5");
        });

        builder.HasKey(review => review.Id);

        builder.Property(review => review.Author)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(review => review.Comment)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(review => review.Rating)
            .IsRequired();

        builder.Property(review => review.MenuRating)
            .IsRequired();
    }
}

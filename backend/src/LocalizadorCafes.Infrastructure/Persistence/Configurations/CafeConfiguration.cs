using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalizadorCafes.Infrastructure.Persistence.Configurations;

public class CafeConfiguration : IEntityTypeConfiguration<Cafe>
{
    public void Configure(EntityTypeBuilder<Cafe> builder)
    {
        builder.ToTable("cafes");

        builder.HasKey(cafe => cafe.Id);

        builder.Property(cafe => cafe.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(cafe => cafe.Address)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(cafe => cafe.Neighborhood)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(cafe => cafe.Phone)
            .HasMaxLength(40);

        builder.Property(cafe => cafe.Hours)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(cafe => cafe.Location)
            .HasColumnType("geography (point)")
            .IsRequired();

        builder.HasIndex(cafe => cafe.Location)
            .HasMethod("GIST");

        builder.HasMany(cafe => cafe.Menu)
            .WithOne(item => item.Cafe)
            .HasForeignKey(item => item.CafeId)
            .OnDelete(DeleteBehavior.Cascade);

        // text[] de PostgreSQL: las etiquetas son un vocabulario corto y fijo.
        builder.Property(cafe => cafe.Tags)
            .HasColumnType("text[]")
            .HasDefaultValueSql("'{}'::text[]")
            .IsRequired();

        builder.HasMany(cafe => cafe.Photos)
            .WithOne(photo => photo.Cafe)
            .HasForeignKey(photo => photo.CafeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cafe => cafe.Reviews)
            .WithOne(review => review.Cafe)
            .HasForeignKey(review => review.CafeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

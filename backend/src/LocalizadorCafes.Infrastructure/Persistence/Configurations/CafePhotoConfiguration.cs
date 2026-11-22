using LocalizadorCafes.Infrastructure.Identity;
using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalizadorCafes.Infrastructure.Persistence.Configurations;

public class CafePhotoConfiguration : IEntityTypeConfiguration<CafePhoto>
{
    public void Configure(EntityTypeBuilder<CafePhoto> builder)
    {
        builder.ToTable("cafe_photos");

        builder.HasKey(photo => photo.Id);

        builder.Property(photo => photo.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(photo => photo.Author)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(photo => photo.License)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(photo => photo.SourceUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(photo => photo.CafeId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(photo => photo.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

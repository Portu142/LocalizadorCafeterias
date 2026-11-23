using LocalizadorCafes.Infrastructure.Identity;
using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalizadorCafes.Infrastructure.Persistence.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(item => item.Price)
            .HasPrecision(10, 2);

        // Se guarda como texto legible («Pending», «Approved»…). Sin valor por
        // defecto en el modelo: Pending es el valor CLR por defecto y EF lo
        // confundiría con «sin asignar».
        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.HasIndex(item => item.Status);

        // Platos añadidos por la comunidad: si se borra la cuenta, el plato se queda.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(item => item.AddedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

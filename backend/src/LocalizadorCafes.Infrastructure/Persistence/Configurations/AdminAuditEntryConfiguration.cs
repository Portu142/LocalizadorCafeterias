using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalizadorCafes.Infrastructure.Persistence.Configurations;

public class AdminAuditEntryConfiguration : IEntityTypeConfiguration<AdminAuditEntry>
{
    public void Configure(EntityTypeBuilder<AdminAuditEntry> builder)
    {
        builder.ToTable("admin_audit_log");
        builder.HasKey(entry => entry.Id);

        // Sin clave foránea a la cuenta: el registro debe sobrevivir aunque se
        // borre el administrador; por eso se guarda también su nombre.
        builder.Property(entry => entry.AdminName).HasMaxLength(60).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(60).IsRequired();
        builder.Property(entry => entry.TargetType).HasMaxLength(20).IsRequired();
        builder.Property(entry => entry.Details).HasMaxLength(300).IsRequired();

        builder.HasIndex(entry => entry.CreatedAt);
        builder.HasIndex(entry => entry.TargetType);
    }
}

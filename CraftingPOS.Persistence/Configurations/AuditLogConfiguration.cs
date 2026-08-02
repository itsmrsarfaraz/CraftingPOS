using CraftingPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CraftingPOS.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Username).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Module).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(1000);

        // FR-AUDIT-002 / SRS Part 4 §21: index for searchability.
        builder.HasIndex(a => a.Username);
        builder.HasIndex(a => a.Module);
        builder.HasIndex(a => a.CreatedAt);
    }
}
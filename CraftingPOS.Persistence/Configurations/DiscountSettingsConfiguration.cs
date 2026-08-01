using CraftingPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CraftingPOS.Persistence.Configurations;

public class DiscountSettingsConfiguration : IEntityTypeConfiguration<DiscountSettings>
{
    public void Configure(EntityTypeBuilder<DiscountSettings> builder)
    {
        builder.ToTable("DiscountSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.MaxCashierDiscountPercent).HasColumnType("decimal(5,2)");
        builder.Property(s => s.MaxCashierDiscountFlat).HasColumnType("decimal(18,2)");
    }
}
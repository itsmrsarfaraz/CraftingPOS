using CraftingPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CraftingPOS.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VariantName).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Barcode).IsRequired().HasMaxLength(64);
        builder.Property(v => v.SKU).IsRequired().HasMaxLength(64);

        builder.Property(v => v.CostPrice).HasColumnType("decimal(18,2)");
        builder.Property(v => v.SellingPrice).HasColumnType("decimal(18,2)");
        builder.Property(v => v.CurrentStock).HasColumnType("decimal(18,3)");
        builder.Property(v => v.MinimumStock).HasColumnType("decimal(18,3)");

        builder.HasIndex(v => v.Barcode);
        builder.HasIndex(v => v.SKU);
    }
}
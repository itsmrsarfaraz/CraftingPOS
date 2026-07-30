using CraftingPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CraftingPOS.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Barcode).IsRequired().HasMaxLength(64);
        builder.Property(p => p.SKU).IsRequired().HasMaxLength(64);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.ImagePath).HasMaxLength(300);

        builder.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.SellingPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CurrentStock).HasColumnType("decimal(18,3)");
        builder.Property(p => p.MinimumStock).HasColumnType("decimal(18,3)");

        // Indexes for search performance (SRS Part 4, Section 21)
        builder.HasIndex(p => p.Barcode);
        builder.HasIndex(p => p.SKU);
        builder.HasIndex(p => p.Name);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
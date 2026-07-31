using CraftingPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CraftingPOS.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Quantity).HasColumnType("decimal(18,3)");
        builder.Property(t => t.StockBefore).HasColumnType("decimal(18,3)");
        builder.Property(t => t.StockAfter).HasColumnType("decimal(18,3)");
        builder.Property(t => t.Notes).HasMaxLength(500);

        builder.HasIndex(t => t.ProductId);

        builder.HasOne(t => t.Product)
            .WithMany()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ProductVariant)
            .WithMany()
            .HasForeignKey(t => t.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
using CraftingPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CraftingPOS.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ProductDiscount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.CartDiscount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.GrandTotal).HasColumnType("decimal(18,2)");

        builder.HasIndex(s => s.InvoiceNumber);
        builder.HasIndex(s => s.SaleDate);

        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(s => s.Cashier)
            .WithMany()
            .HasForeignKey(s => s.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Sale)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Payments)
            .WithOne(p => p.Sale)
            .HasForeignKey(p => p.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
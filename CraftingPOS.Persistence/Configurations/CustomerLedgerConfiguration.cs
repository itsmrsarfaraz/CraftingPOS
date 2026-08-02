using CraftingPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CraftingPOS.Persistence.Configurations;

public class CustomerLedgerConfiguration : IEntityTypeConfiguration<CustomerLedger>
{
    public void Configure(EntityTypeBuilder<CustomerLedger> builder)
    {
        builder.ToTable("CustomerLedgers");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Debit).HasColumnType("decimal(18,2)");
        builder.Property(l => l.Credit).HasColumnType("decimal(18,2)");
        builder.Property(l => l.Balance).HasColumnType("decimal(18,2)");
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasIndex(l => l.CustomerId);

        builder.HasOne(l => l.Customer)
            .WithMany()
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // SaleId is intentionally a plain nullable int with no FK constraint —
        // the Sales table doesn't exist until Sprint 10. It will be populated
        // then without needing a schema change to this table.
    }
}
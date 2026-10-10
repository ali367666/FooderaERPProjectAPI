using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class CounterpartyDebtEntryConfiguration : IEntityTypeConfiguration<CounterpartyDebtEntry>
{
    public void Configure(EntityTypeBuilder<CounterpartyDebtEntry> builder)
    {
        builder.ToTable("CounterpartyDebtEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(300);

        builder.HasOne(x => x.Counterparty)
            .WithMany()
            .HasForeignKey(x => x.CounterpartyId)
            // Restrict: the company -> counterparty cascade already reaches this table, and SQL Server
            // refuses a second cascade path.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CounterpartyId, x.CreatedAtUtc });
    }
}

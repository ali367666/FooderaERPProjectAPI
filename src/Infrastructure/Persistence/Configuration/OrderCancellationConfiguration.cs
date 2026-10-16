using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class OrderCancellationConfiguration : IEntityTypeConfiguration<OrderCancellation>
{
    public void Configure(EntityTypeBuilder<OrderCancellation> builder)
    {
        builder.ToTable("OrderCancellations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ReceiptNumber).HasMaxLength(50);
        builder.Property(x => x.TableName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.MenuItemName).HasMaxLength(200);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.CancelledByName).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
    }
}

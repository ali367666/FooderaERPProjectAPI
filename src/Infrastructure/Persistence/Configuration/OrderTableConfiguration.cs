using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class OrderTableConfiguration : IEntityTypeConfiguration<OrderTable>
{
    public void Configure(EntityTypeBuilder<OrderTable> builder)
    {
        builder.ToTable("OrderTables");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Order)
            .WithMany(o => o.BanquetTables)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Table)
            .WithMany()
            .HasForeignKey(x => x.TableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrderId, x.TableId }).IsUnique();
        builder.HasIndex(x => x.TableId);
    }
}

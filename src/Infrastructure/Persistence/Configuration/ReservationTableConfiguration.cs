using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ReservationTableConfiguration : IEntityTypeConfiguration<ReservationTable>
{
    public void Configure(EntityTypeBuilder<ReservationTable> builder)
    {
        builder.ToTable("ReservationTables");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Reservation)
            .WithMany(r => r.BanquetTables)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Table)
            .WithMany()
            .HasForeignKey(x => x.TableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ReservationId, x.TableId }).IsUnique();
        builder.HasIndex(x => x.TableId);
    }
}

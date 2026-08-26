using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class PrinterStationTypeConfiguration : IEntityTypeConfiguration<PrinterStationType>
{
    public void Configure(EntityTypeBuilder<PrinterStationType> builder)
    {
        builder.ToTable("PrinterStationTypes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new { x.CompanyId, x.Name })
            .IsUnique();
    }
}

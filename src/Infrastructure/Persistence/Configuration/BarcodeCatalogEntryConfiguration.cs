using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class BarcodeCatalogEntryConfiguration : IEntityTypeConfiguration<BarcodeCatalogEntry>
{
    public void Configure(EntityTypeBuilder<BarcodeCatalogEntry> builder)
    {
        builder.ToTable("BarcodeCatalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.Barcode).IsUnique();
    }
}

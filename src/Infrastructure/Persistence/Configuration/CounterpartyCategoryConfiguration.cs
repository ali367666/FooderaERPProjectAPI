using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class CounterpartyCategoryConfiguration : IEntityTypeConfiguration<CounterpartyCategory>
{
    public void Configure(EntityTypeBuilder<CounterpartyCategory> builder)
    {
        builder.ToTable("CounterpartyCategories");

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

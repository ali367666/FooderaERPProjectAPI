using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.CompanyCode)
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.TaxOfficeCode)
            .HasMaxLength(50);

        builder.Property(x => x.TaxNumber)
            .HasMaxLength(50);

        builder.Property(x => x.CountryCode)
            .HasMaxLength(20);

        builder.Property(x => x.Country)
            .HasMaxLength(100);

        builder.Property(x => x.PrimaryPhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.SecondaryPhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.HasMany(x => x.Restaurants)
            .WithOne(x => x.Company)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
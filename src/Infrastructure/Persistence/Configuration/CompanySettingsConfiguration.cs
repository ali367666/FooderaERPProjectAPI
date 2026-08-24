using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LoginLogoUrl).HasMaxLength(500);
        builder.Property(x => x.ReportLogoUrl).HasMaxLength(500);
        builder.Property(x => x.WallpaperUrl).HasMaxLength(500);
        builder.Property(x => x.LoginLocation).HasMaxLength(100);
        builder.Property(x => x.ProductColor).HasMaxLength(20);
        builder.Property(x => x.FloorLabel).HasMaxLength(100);
        builder.Property(x => x.Slogan).HasMaxLength(300);
        builder.Property(x => x.SocialLinks).HasMaxLength(1000);
        builder.Property(x => x.ContactPhoneNumber).HasMaxLength(30);

        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        // hər company üçün tək tənzimləmə sətri
        builder.HasIndex(x => x.CompanyId)
               .IsUnique();
    }
}

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CompanyEmailSettingsConfiguration : IEntityTypeConfiguration<CompanyEmailSettings>
{
    public void Configure(EntityTypeBuilder<CompanyEmailSettings> builder)
    {
        builder.ToTable("CompanyEmailSettings");
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.ProtectedPassword).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.SenderName).HasMaxLength(100);
        builder.HasIndex(x => x.CompanyId).IsUnique();
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

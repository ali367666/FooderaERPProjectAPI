using Domain.Entities.StoreMode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StoreSyncOperationConfiguration : IEntityTypeConfiguration<StoreSyncOperation>
{
    public void Configure(EntityTypeBuilder<StoreSyncOperation> b)
    {
        b.ToTable("StoreSyncOperations");
        b.HasKey(x => x.Id);
        b.Property(x => x.DeviceId).IsRequired().HasMaxLength(100);
        b.Property(x => x.Type).IsRequired().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.ResultDocumentNo).HasMaxLength(50);
        b.Property(x => x.Error).HasMaxLength(1000);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.Status });
    }
}

public class StoreUserBranchConfiguration : IEntityTypeConfiguration<StoreUserBranch>
{
    public void Configure(EntityTypeBuilder<StoreUserBranch> b)
    {
        b.ToTable("StoreUserBranches");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.UserId, x.RestaurantId }).IsUnique();
    }
}

public class StoreBranchPriceConfiguration : IEntityTypeConfiguration<StoreBranchPrice>
{
    public void Configure(EntityTypeBuilder<StoreBranchPrice> b)
    {
        b.ToTable("StoreBranchPrices");
        b.HasKey(x => x.Id);
        b.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.RestaurantId, x.StoreProductId }).IsUnique();
    }
}

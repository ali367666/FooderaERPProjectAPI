using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class MenuItemRecipeLineConfiguration : IEntityTypeConfiguration<MenuItemRecipeLine>
{
    public void Configure(EntityTypeBuilder<MenuItemRecipeLine> builder)
    {
        builder.ToTable("MenuItemRecipeLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityPerPortion)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.Unit)
            .IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.MenuItemId, x.StockItemId })
            .IsUnique();

        builder.HasOne(x => x.MenuItem)
            .WithMany(x => x.RecipeLines)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.StockItem)
            .WithMany(x => x.RecipeLines)
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

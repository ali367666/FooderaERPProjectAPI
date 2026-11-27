using Application.StoreMode.Reports;
using Domain.Enums;
using Xunit;

namespace Tests;

public class StoreProductFlowTests
{
    [Theory]
    [InlineData(StoreStockMovementType.Purchase, "purchase")]
    [InlineData(StoreStockMovementType.Sale, "sale")]
    [InlineData(StoreStockMovementType.SaleReturn, "saleReturn")]
    [InlineData(StoreStockMovementType.SaleCancel, "saleReturn")]
    [InlineData(StoreStockMovementType.PurchaseReturn, "purchaseReturn")]
    [InlineData(StoreStockMovementType.TransferIn, "transferIn")]
    [InlineData(StoreStockMovementType.TransferOut, "transferOut")]
    [InlineData(StoreStockMovementType.AdjustmentIncrease, "adjustUp")]
    [InlineData(StoreStockMovementType.AdjustmentDecrease, "adjustDown")]
    [InlineData(StoreStockMovementType.Waste, "waste")]
    [InlineData(StoreStockMovementType.InternalUse, "internalUse")]
    public void Every_movement_kind_has_its_own_column(StoreStockMovementType type, string bucket)
    {
        Assert.Equal(bucket, StoreProductFlowHandler.BucketOf(type));
        Assert.Contains(bucket, StoreProductFlowHandler.Buckets);
    }

    [Fact]
    public void Every_movement_kind_maps_to_a_known_column()
    {
        foreach (var type in Enum.GetValues<StoreStockMovementType>())
            Assert.Contains(StoreProductFlowHandler.BucketOf(type), StoreProductFlowHandler.Buckets);
    }
}

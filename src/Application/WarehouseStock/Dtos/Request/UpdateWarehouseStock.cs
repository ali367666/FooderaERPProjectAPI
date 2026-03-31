namespace Application.WarehouseStock.Dtos.Request;

public class UpdateWarehouseStockRequest
{
    public decimal? QuantityOnHand { get; set; }
    public decimal? MinLevel { get; set; }
}
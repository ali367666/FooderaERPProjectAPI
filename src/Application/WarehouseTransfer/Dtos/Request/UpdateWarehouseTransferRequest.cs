namespace Application.WarehouseTransfers.Dtos.Request;

public class UpdateWarehouseTransferRequest
{
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }

    public int? VehicleWarehouseId { get; set; }

    public string? Note { get; set; }

    public List<UpdateWarehouseTransferLineRequest> Lines { get; set; } = new();
}
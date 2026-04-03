using Application.WarehouseTransfer.Dtos.Request;

namespace Application.WarehouseTransfers.Dtos.Request;

public class CreateWarehouseTransferRequest
{
    public int CompanyId { get; set; }

    public int? StockRequestId { get; set; }

    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }

    public int? VehicleWarehouseId { get; set; }

    public string? Note { get; set; }

    public List<CreateWarehouseTransferLineRequest> Lines { get; set; } = new();
}
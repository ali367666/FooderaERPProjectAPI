using Domain.Enums;

namespace Application.StockRequests.Dtos.Response;

public class StockRequestResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    public int RequestingWarehouseId { get; set; }
    public string RequestingWarehouseName { get; set; } = default!;

    public int SupplyingWarehouseId { get; set; }
    public string SupplyingWarehouseName { get; set; } = default!;

    public StockRequestStatus Status { get; set; }
    public string? Note { get; set; }

    public List<StockRequestLineResponse> Lines { get; set; } = new();
}
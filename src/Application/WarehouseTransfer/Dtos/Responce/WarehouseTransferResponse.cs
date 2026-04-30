using Domain.Enums;

namespace Application.WarehouseTransfers.Dtos.Response;

public class WarehouseTransferResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    public string DocumentNo { get; set; } = default!;

    public int? StockRequestId { get; set; }

    public int FromWarehouseId { get; set; }
    public string FromWarehouseName { get; set; } = default!;

    public int ToWarehouseId { get; set; }
    public string ToWarehouseName { get; set; } = default!;

    public int? VehicleWarehouseId { get; set; }
    public string? VehicleWarehouseName { get; set; }

    public TransferStatus Status { get; set; }

    public string? Note { get; set; }

    public DateTime TransferDate { get; set; }

    public List<WarehouseTransferLineResponse> Lines { get; set; } = new();
}
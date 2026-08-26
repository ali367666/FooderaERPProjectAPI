using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseStock.Commands.PosAdjust;

public class PosAdjustWarehouseStockRequest
{
    public int WarehouseId { get; set; }
    public int StockItemId { get; set; }
    public decimal NewQuantity { get; set; }
    public int UnitId { get; set; }
}

public record PosAdjustWarehouseStockCommand(PosAdjustWarehouseStockRequest Request) : IRequest<BaseResponse>;

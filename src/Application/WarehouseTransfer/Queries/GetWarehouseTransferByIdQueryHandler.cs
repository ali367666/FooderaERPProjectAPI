using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseTransfers.Dtos.Response;
using MediatR;

namespace Application.WarehouseTransfer.Queries.GetById;

public class GetWarehouseTransferByIdQueryHandler
    : IRequestHandler<GetWarehouseTransferByIdQuery, BaseResponse<WarehouseTransferResponse>>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public GetWarehouseTransferByIdQueryHandler(
        IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse<WarehouseTransferResponse>> Handle(
        GetWarehouseTransferByIdQuery request,
        CancellationToken cancellationToken)
    {
        var warehouseTransfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (warehouseTransfer is null)
        {
            return new BaseResponse<WarehouseTransferResponse>
            {
                Success = false,
                Message = "Warehouse transfer tapılmadı."
            };
        }

        var response = new WarehouseTransferResponse
        {
            Id = warehouseTransfer.Id,
            CompanyId = warehouseTransfer.CompanyId,
            StockRequestId = warehouseTransfer.StockRequestId,

            FromWarehouseId = warehouseTransfer.FromWarehouseId,
            FromWarehouseName = warehouseTransfer.FromWarehouse?.Name ?? string.Empty,

            ToWarehouseId = warehouseTransfer.ToWarehouseId,
            ToWarehouseName = warehouseTransfer.ToWarehouse?.Name ?? string.Empty,

            VehicleWarehouseId = warehouseTransfer.VehicleWarehouseId,
            VehicleWarehouseName = warehouseTransfer.VehicleWarehouse?.Name,

            Status = warehouseTransfer.Status,
            Note = warehouseTransfer.Note,
            TransferDate = warehouseTransfer.TransferDate,

            Lines = warehouseTransfer.Lines.Select(x => new WarehouseTransferLineResponse
            {
                Id = x.Id,
                StockItemId = x.StockItemId,
                StockItemName = x.StockItem?.Name ?? string.Empty,
                Quantity = x.Quantity
            }).ToList()
        };

        return new BaseResponse<WarehouseTransferResponse>
        {
            Success = true,
            Message = "Warehouse transfer uğurla gətirildi.",
            Data = response
        };
    }
}
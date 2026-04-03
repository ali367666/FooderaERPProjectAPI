using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseTransfers.Dtos.Response;
using MediatR;

namespace Application.WarehouseTransfer.Queries.GetAll;

public class GetAllWarehouseTransfersQueryHandler
    : IRequestHandler<GetAllWarehouseTransfersQuery, BaseResponse<List<WarehouseTransferResponse>>>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public GetAllWarehouseTransfersQueryHandler(
        IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse<List<WarehouseTransferResponse>>> Handle(
        GetAllWarehouseTransfersQuery request,
        CancellationToken cancellationToken)
    {
        var warehouseTransfers = await _warehouseTransferRepository
            .GetAllWithDetailsAsync(cancellationToken);

        var response = warehouseTransfers.Select(warehouseTransfer => new WarehouseTransferResponse
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
        }).ToList();

        return new BaseResponse<List<WarehouseTransferResponse>>
        {
            Success = true,
            Message = "Warehouse transferlər uğurla gətirildi.",
            Data = response
        };
    }
}
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfers.Commands.Create;

public class CreateWarehouseTransferCommandHandler
    : IRequestHandler<CreateWarehouseTransferCommand, BaseResponse<int>>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public CreateWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var duplicateStockItemIds = dto.Lines
            .GroupBy(x => x.StockItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateStockItemIds.Any())
        {
            return new BaseResponse<int>
            {
                Success = false,
                Message = "Eyni StockItem bir transfer daxilində bir neçə dəfə göndərilə bilməz."
            };
        }

        var warehouseTransfer = new Domain.Entities.WarehouseAndStock.WarehouseTransfer
        {
            CompanyId = dto.CompanyId,
            StockRequestId = dto.StockRequestId,
            FromWarehouseId = dto.FromWarehouseId,
            ToWarehouseId = dto.ToWarehouseId,
            VehicleWarehouseId = dto.VehicleWarehouseId,
            Note = dto.Note,
            Status = TransferStatus.Draft,
            TransferDate = DateTime.UtcNow,
            Lines = dto.Lines.Select(x => new Domain.Entities.WarehouseAndStock.WarehouseTransferLine
            {
                CompanyId = dto.CompanyId,
                StockItemId = x.StockItemId,
                Quantity = x.Quantity
            }).ToList()
        };

        await _warehouseTransferRepository.AddAsync(warehouseTransfer, cancellationToken);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse<int>
        {
            Success = true,
            Message = "Warehouse transfer uğurla yaradıldı.",
            Data = warehouseTransfer.Id
        };
    }
}
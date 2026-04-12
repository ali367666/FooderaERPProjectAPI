using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Update;

public class UpdateWarehouseTransferCommandHandler
    : IRequestHandler<UpdateWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public UpdateWarehouseTransferCommandHandler(IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse> Handle(
        UpdateWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status != TransferStatus.Draft)
            return BaseResponse.Fail("Only draft warehouse transfers can be updated.");

        var fromWarehouseExists = await _warehouseTransferRepository
            .WarehouseExistsAsync(request.Request.FromWarehouseId, cancellationToken);

        if (!fromWarehouseExists)
            return BaseResponse.Fail("From warehouse not found.");

        var toWarehouseExists = await _warehouseTransferRepository
            .WarehouseExistsAsync(request.Request.ToWarehouseId, cancellationToken);

        if (!toWarehouseExists)
            return BaseResponse.Fail("To warehouse not found.");

        var stockItemIds = request.Request.Lines
            .Select(x => x.StockItemId)
            .Distinct()
            .ToList();

        var existingStockItemIds = await _warehouseTransferRepository
            .GetExistingStockItemIdsAsync(stockItemIds, cancellationToken);

        var missingStockItemIds = stockItemIds.Except(existingStockItemIds).ToList();

        if (missingStockItemIds.Any())
            return BaseResponse.Fail($"Stock item(s) not found: {string.Join(", ", missingStockItemIds)}");

        transfer.FromWarehouseId = request.Request.FromWarehouseId;
        transfer.ToWarehouseId = request.Request.ToWarehouseId;
        transfer.Note = request.Request.Note;

        if (transfer.Lines.Any())
            _warehouseTransferRepository.RemoveLines(transfer.Lines);

        transfer.Lines = request.Request.Lines
            .Select(x => new WarehouseTransferLine
            {
                CompanyId = transfer.CompanyId,
                StockItemId = x.StockItemId,
                Quantity = x.Quantity
            })
            .ToList();

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Warehouse transfer updated successfully.");
    }
}
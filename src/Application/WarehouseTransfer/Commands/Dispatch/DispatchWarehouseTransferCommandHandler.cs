using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Dispatch;

public class DispatchWarehouseTransferCommandHandler
    : IRequestHandler<DispatchWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;

    public DispatchWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IWarehouseStockRepository warehouseStockRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _warehouseStockRepository = warehouseStockRepository;
    }

    public async Task<BaseResponse> Handle(
        DispatchWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status != TransferStatus.Approved)
            return BaseResponse.Fail("Only approved warehouse transfers can be dispatched.");

        if (transfer.Lines is null || !transfer.Lines.Any())
            return BaseResponse.Fail("Warehouse transfer has no lines.");

        foreach (var line in transfer.Lines)
        {
            var warehouseStock = await _warehouseStockRepository
                .GetByWarehouseAndStockItemAsync(
                    transfer.FromWarehouseId,
                    line.StockItemId,
                    cancellationToken);

            if (warehouseStock is null)
                return BaseResponse.Fail($"Stock item {line.StockItemId} was not found in source warehouse.");

            if (warehouseStock.QuantityOnHand < line.Quantity)
                return BaseResponse.Fail(
                    $"Insufficient stock for stock item {line.StockItemId}. Available: {warehouseStock.QuantityOnHand}, Requested: {line.Quantity}");
        }

        await using var transaction = await _warehouseTransferRepository
            .BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in transfer.Lines)
            {
                var warehouseStock = await _warehouseStockRepository
                    .GetByWarehouseAndStockItemAsync(
                        transfer.FromWarehouseId,
                        line.StockItemId,
                        cancellationToken);

                warehouseStock!.QuantityOnHand -= line.Quantity;
                _warehouseStockRepository.Update(warehouseStock);
            }

            transfer.Status = TransferStatus.InTransit;
            _warehouseTransferRepository.Update(transfer);

            await _warehouseStockRepository.SaveChangesAsync(cancellationToken);
            await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return BaseResponse.Ok("Warehouse transfer dispatched successfully.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
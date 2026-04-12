using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Receive;

public class ReceiveWarehouseTransferCommandHandler
    : IRequestHandler<ReceiveWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;

    public ReceiveWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IWarehouseStockRepository warehouseStockRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _warehouseStockRepository = warehouseStockRepository;
    }

    public async Task<BaseResponse> Handle(
        ReceiveWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status != TransferStatus.InTransit)
            return BaseResponse.Fail("Only in-transit warehouse transfers can be received.");

        if (transfer.Lines is null || !transfer.Lines.Any())
            return BaseResponse.Fail("Warehouse transfer has no lines.");

        await using var transaction = await _warehouseTransferRepository
            .BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in transfer.Lines)
            {
                var warehouseStock = await _warehouseStockRepository
                    .GetByWarehouseAndStockItemAsync(
                        transfer.ToWarehouseId,
                        line.StockItemId,
                        cancellationToken);

                if (warehouseStock is null)
                {
                    var newStock = new Domain.Entities.WarehouseAndStock.WarehouseStock
                    {
                        CompanyId = transfer.CompanyId,
                        WarehouseId = transfer.ToWarehouseId,
                        StockItemId = line.StockItemId,
                        QuantityOnHand = line.Quantity
                    };

                    await _warehouseStockRepository.AddAsync(newStock, cancellationToken);
                }
                else
                {
                    warehouseStock.QuantityOnHand += line.Quantity;
                    _warehouseStockRepository.Update(warehouseStock);
                }
            }

            transfer.Status = TransferStatus.Completed;
            _warehouseTransferRepository.Update(transfer);

            await _warehouseStockRepository.SaveChangesAsync(cancellationToken);
            await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return BaseResponse.Ok("Warehouse transfer received successfully.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
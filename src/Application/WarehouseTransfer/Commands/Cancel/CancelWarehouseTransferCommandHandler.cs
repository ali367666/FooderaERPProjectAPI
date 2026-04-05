using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Cancel;

public class CancelWarehouseTransferCommandHandler
    : IRequestHandler<CancelWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public CancelWarehouseTransferCommandHandler(IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse> Handle(
        CancelWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await _warehouseTransferRepository
            .GetByIdAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status == TransferStatus.InTransit)
            return BaseResponse.Fail("In-transit warehouse transfers cannot be cancelled.");

        if (transfer.Status == TransferStatus.Completed)
            return BaseResponse.Fail("Completed warehouse transfers cannot be cancelled.");

        if (transfer.Status == TransferStatus.Rejected)
            return BaseResponse.Fail("Rejected warehouse transfers cannot be cancelled.");

        if (transfer.Status == TransferStatus.Cancelled)
            return BaseResponse.Fail("Warehouse transfer is already cancelled.");

        transfer.Status = TransferStatus.Cancelled;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Warehouse transfer cancelled successfully.");
    }
}
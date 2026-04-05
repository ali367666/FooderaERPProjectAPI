using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Reject;

public class RejectWarehouseTransferCommandHandler
    : IRequestHandler<RejectWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public RejectWarehouseTransferCommandHandler(IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse> Handle(
        RejectWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await _warehouseTransferRepository
            .GetByIdAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status != TransferStatus.Pending)
            return BaseResponse.Fail("Only pending warehouse transfers can be rejected.");

        transfer.Status = TransferStatus.Rejected;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Warehouse transfer rejected successfully.");
    }
}
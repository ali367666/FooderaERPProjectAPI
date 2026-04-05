using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Approve;

public class ApproveWarehouseTransferCommandHandler
    : IRequestHandler<ApproveWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public ApproveWarehouseTransferCommandHandler(IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse> Handle(
        ApproveWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status != TransferStatus.Pending)
            return BaseResponse.Fail("Only pending warehouse transfers can be approved.");

        if (transfer.Lines is null || !transfer.Lines.Any())
            return BaseResponse.Fail("Warehouse transfer must contain at least one line.");

        if (transfer.FromWarehouseId == transfer.ToWarehouseId)
            return BaseResponse.Fail("From warehouse and To warehouse cannot be the same.");

        if (transfer.Lines.Any(x => x.Quantity <= 0))
            return BaseResponse.Fail("All line quantities must be greater than 0.");

        transfer.Status = TransferStatus.Approved;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Warehouse transfer approved successfully.");
    }
}
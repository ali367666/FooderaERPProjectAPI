using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Submit;

public class SubmitWarehouseTransferCommandHandler
    : IRequestHandler<SubmitWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public SubmitWarehouseTransferCommandHandler(IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse> Handle(
        SubmitWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status != TransferStatus.Draft)
            return BaseResponse.Fail("Only draft warehouse transfers can be submitted.");

        if (transfer.FromWarehouseId == transfer.ToWarehouseId)
            return BaseResponse.Fail("From warehouse and To warehouse cannot be the same.");

        if (transfer.Lines is null || !transfer.Lines.Any())
            return BaseResponse.Fail("Warehouse transfer must contain at least one line.");

        if (transfer.Lines.Any(x => x.Quantity <= 0))
            return BaseResponse.Fail("All line quantities must be greater than 0.");

        transfer.Status = TransferStatus.Pending;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Warehouse transfer submitted successfully.");
    }
}
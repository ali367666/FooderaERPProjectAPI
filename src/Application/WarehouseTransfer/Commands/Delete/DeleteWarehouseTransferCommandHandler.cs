using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Delete;

public class DeleteWarehouseTransferCommandHandler
    : IRequestHandler<DeleteWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;

    public DeleteWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
    }

    public async Task<BaseResponse> Handle(
        DeleteWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var warehouseTransfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (warehouseTransfer is null)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "Warehouse transfer tapılmadı."
            };
        }

        if (warehouseTransfer.Status != TransferStatus.Draft)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "Yalnız Draft statusunda olan transfer silinə bilər."
            };
        }

        await _warehouseTransferRepository.DeleteAsync(warehouseTransfer, cancellationToken);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse
        {
            Success = true,
            Message = "Warehouse transfer uğurla silindi."
        };
    }
}
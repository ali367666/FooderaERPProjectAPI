using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.StockRequests.Commands.Submit;

public class SubmitStockRequestCommandHandler
    : IRequestHandler<SubmitStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(
        SubmitStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        Domain.Entities.StockRequest? entity = await _stockRequestRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (entity is null)
            return BaseResponse.Fail("Stock request not found.");

        if (entity.Status != StockRequestStatus.Draft)
            return BaseResponse.Fail("Only draft requests can be submitted.");

        if (entity.Lines is null || !entity.Lines.Any())
            return BaseResponse.Fail("Stock request must contain at least one line.");

        if (entity.RequestingWarehouseId == entity.SupplyingWarehouseId)
            return BaseResponse.Fail("Requesting warehouse and supplying warehouse cannot be the same.");

        entity.Status = StockRequestStatus.Submitted;

        _stockRequestRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Stock request submitted successfully.");
    }
}
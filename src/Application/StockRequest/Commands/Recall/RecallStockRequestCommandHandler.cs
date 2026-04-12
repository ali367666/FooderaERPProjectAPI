using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.StockRequests.Commands.Recall;

public class RecallStockRequestCommandHandler
    : IRequestHandler<RecallStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecallStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(
        RecallStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        Domain.Entities.WarehouseAndStock.StockRequest? entity = await _stockRequestRepository
            .GetByIdAsync(request.Id, cancellationToken);

        if (entity is null)
            return BaseResponse.Fail("Stock request not found.");

        if (entity.Status != StockRequestStatus.Submitted)
            return BaseResponse.Fail("Only submitted requests can be recalled.");

        entity.Status = StockRequestStatus.Draft;

        _stockRequestRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Stock request recalled successfully.");
    }
}
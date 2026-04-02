using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.StockRequests.Commands.Reject;

public class RejectStockRequestCommandHandler
    : IRequestHandler<RejectStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(
        RejectStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        Domain.Entities.StockRequest? entity = await _stockRequestRepository
            .GetByIdAsync(request.Id, cancellationToken);

        if (entity is null)
            return BaseResponse.Fail("Stock request not found.");

        if (entity.Status != StockRequestStatus.Submitted)
            return BaseResponse.Fail("Only submitted requests can be rejected.");

        entity.Status = StockRequestStatus.Rejected;

        _stockRequestRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Stock request rejected successfully.");
    }
}
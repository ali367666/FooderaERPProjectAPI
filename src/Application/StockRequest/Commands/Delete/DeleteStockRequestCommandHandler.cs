using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.StockRequests.Commands.Delete;

public class DeleteStockRequestCommandHandler
    : IRequestHandler<DeleteStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;

    public DeleteStockRequestCommandHandler(IStockRequestRepository stockRequestRepository)
    {
        _stockRequestRepository = stockRequestRepository;
    }

    public async Task<BaseResponse> Handle(
        DeleteStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        var stockRequest = await _stockRequestRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (stockRequest is null)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "Stock request tapılmadı."
            };
        }

        if (stockRequest.Status != StockRequestStatus.Draft)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "Yalnız Draft statusunda olan request silinə bilər."
            };
        }

        await _stockRequestRepository.DeleteAsync(stockRequest, cancellationToken);
        await _stockRequestRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse
        {
            Success = true,
            Message = "Stock request uğurla silindi."
        };
    }
}
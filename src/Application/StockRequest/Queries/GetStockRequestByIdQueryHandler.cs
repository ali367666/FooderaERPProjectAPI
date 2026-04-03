using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockRequests.Dtos.Response;
using MediatR;

namespace Application.StockRequests.Queries.GetById;

public class GetStockRequestByIdQueryHandler
    : IRequestHandler<GetStockRequestByIdQuery, BaseResponse<StockRequestResponse>>
{
    private readonly IStockRequestRepository _stockRequestRepository;

    public GetStockRequestByIdQueryHandler(IStockRequestRepository stockRequestRepository)
    {
        _stockRequestRepository = stockRequestRepository;
    }

    public async Task<BaseResponse<StockRequestResponse>> Handle(
        GetStockRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var stockRequest = await _stockRequestRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (stockRequest is null)
        {
            return new BaseResponse<StockRequestResponse>
            {
                Success = false,
                Message = "Stock request tapılmadı."
            };
        }

        var response = new StockRequestResponse
        {
            Id = stockRequest.Id,
            CompanyId = stockRequest.CompanyId,
            RequestingWarehouseId = stockRequest.RequestingWarehouseId,
            RequestingWarehouseName = stockRequest.RequestingWarehouse?.Name ?? string.Empty,
            SupplyingWarehouseId = stockRequest.SupplyingWarehouseId,
            SupplyingWarehouseName = stockRequest.SupplyingWarehouse?.Name ?? string.Empty,
            Status = stockRequest.Status,
            Note = stockRequest.Note,
            Lines = stockRequest.Lines.Select(x => new StockRequestLineResponse
            {
                Id = x.Id,
                StockItemId = x.StockItemId,
                StockItemName = x.StockItem?.Name ?? string.Empty,
                Quantity = x.Quantity
            }).ToList()
        };

        return new BaseResponse<StockRequestResponse>
        {
            Success = true,
            Message = "Stock request uğurla gətirildi.",
            Data = response
        };
    }
}
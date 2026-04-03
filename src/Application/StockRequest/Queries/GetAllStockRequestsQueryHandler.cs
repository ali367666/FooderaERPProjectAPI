using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockRequests.Dtos.Response;
using MediatR;

namespace Application.StockRequests.Queries.GetAll;

public class GetAllStockRequestsQueryHandler
    : IRequestHandler<GetAllStockRequestsQuery, BaseResponse<List<StockRequestResponse>>>
{
    private readonly IStockRequestRepository _stockRequestRepository;

    public GetAllStockRequestsQueryHandler(IStockRequestRepository stockRequestRepository)
    {
        _stockRequestRepository = stockRequestRepository;
    }

    public async Task<BaseResponse<List<StockRequestResponse>>> Handle(
        GetAllStockRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var stockRequests = await _stockRequestRepository
            .GetAllWithDetailsAsync(cancellationToken);

        var response = stockRequests.Select(stockRequest => new StockRequestResponse
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
        }).ToList();

        return new BaseResponse<List<StockRequestResponse>>
        {
            Success = true,
            Message = "Stock requestlər uğurla gətirildi.",
            Data = response
        };
    }
}
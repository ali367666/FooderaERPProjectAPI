using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockRequests.Dtos.Response;
using MediatR;

namespace Application.StockRequests.Queries.GetAll;

public class GetAllStockRequestsQueryHandler
    : IRequestHandler<GetAllStockRequestsQuery, BaseResponse<List<StockRequestResponse>>>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllStockRequestsQueryHandler(
        IStockRequestRepository stockRequestRepository,
        ICurrentUserService currentUserService)
    {
        _stockRequestRepository = stockRequestRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<StockRequestResponse>>> Handle(
        GetAllStockRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var stockRequests = await _stockRequestRepository
            .GetAllWithDetailsAsync(cancellationToken);

        // A tenant Admin only ever sees their own company's inter-warehouse stock requests.
        // SuperAdmin can target one company via CompanyId, or omit it to browse every company.
        if (!_currentUserService.IsSuperAdmin)
        {
            stockRequests = stockRequests.Where(x => x.CompanyId == _currentUserService.CompanyId).ToList();
        }
        else if (request.CompanyId is > 0)
        {
            stockRequests = stockRequests.Where(x => x.CompanyId == request.CompanyId.Value).ToList();
        }

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
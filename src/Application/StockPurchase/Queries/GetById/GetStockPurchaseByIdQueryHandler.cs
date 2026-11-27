using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockPurchase.Dtos.Response;
using MediatR;

namespace Application.StockPurchase.Queries.GetById;

public class GetStockPurchaseByIdQueryHandler
    : IRequestHandler<GetStockPurchaseByIdQuery, BaseResponse<StockPurchaseResponse>>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetStockPurchaseByIdQueryHandler(
        IStockPurchaseRepository purchaseRepository,
        ICurrentUserService currentUserService)
    {
        _purchaseRepository = purchaseRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<StockPurchaseResponse>> Handle(
        GetStockPurchaseByIdQuery request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null || (!_currentUserService.IsSuperAdmin && purchase.CompanyId != _currentUserService.CompanyId))
            return new BaseResponse<StockPurchaseResponse> { Success = false, Message = "Stok alışı tapılmadı." };

        var userNames = await _purchaseRepository.GetUserNamesAsync(
            purchase.Payments.Select(x => x.CreatedByUserId)
                .Concat(purchase.Returns.Select(r => r.CreatedByUserId))
                .Append(purchase.CreatedByUserId)
                .OfType<int>(),
            cancellationToken);

        return new BaseResponse<StockPurchaseResponse>
        {
            Success = true,
            Data = StockPurchaseMapper.ToResponse(purchase, userNames)
        };
    }
}

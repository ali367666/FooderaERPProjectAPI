using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockPurchase.Dtos.Response;
using MediatR;

namespace Application.StockPurchase.Queries.GetAll;

public class GetAllStockPurchasesQueryHandler
    : IRequestHandler<GetAllStockPurchasesQuery, BaseResponse<List<StockPurchaseResponse>>>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllStockPurchasesQueryHandler(
        IStockPurchaseRepository purchaseRepository,
        ICurrentUserService currentUserService)
    {
        _purchaseRepository = purchaseRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<StockPurchaseResponse>>> Handle(
        GetAllStockPurchasesQuery request, CancellationToken cancellationToken)
    {
        var purchases = await _purchaseRepository.GetAllAsync(cancellationToken);

        // A tenant Admin only ever sees their own company's purchase documents. SuperAdmin can
        // target one company via CompanyId, or omit it to browse every company.
        if (!_currentUserService.IsSuperAdmin)
        {
            purchases = purchases.Where(p => p.CompanyId == _currentUserService.CompanyId).ToList();
        }
        else if (request.CompanyId is > 0)
        {
            purchases = purchases.Where(p => p.CompanyId == request.CompanyId.Value).ToList();
        }

        var userNames = await _purchaseRepository.GetUserNamesAsync(
            purchases.SelectMany(p => p.Payments.Select(x => x.CreatedByUserId)
                    .Concat(p.Returns.Select(r => r.CreatedByUserId))
                    .Append(p.CreatedByUserId))
                .OfType<int>(),
            cancellationToken);

        var result = purchases.Select(p => StockPurchaseMapper.ToResponse(p, userNames)).ToList();

        return new BaseResponse<List<StockPurchaseResponse>> { Success = true, Data = result };
    }
}

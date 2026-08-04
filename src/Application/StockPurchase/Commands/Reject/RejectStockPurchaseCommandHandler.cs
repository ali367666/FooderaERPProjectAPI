using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.StockPurchase.Commands.Reject;

public class RejectStockPurchaseCommandHandler
    : IRequestHandler<RejectStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;

    public RejectStockPurchaseCommandHandler(IStockPurchaseRepository purchaseRepository)
    {
        _purchaseRepository = purchaseRepository;
    }

    public async Task<BaseResponse> Handle(RejectStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null)
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Pending)
            return new BaseResponse { Success = false, Message = "Yalnız gözləmədəki alışlar rədd edilə bilər." };

        purchase.Status = StockPurchaseStatus.Rejected;
        _purchaseRepository.Update(purchase);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse { Success = true, Message = "Stok alışı rədd edildi." };
    }
}

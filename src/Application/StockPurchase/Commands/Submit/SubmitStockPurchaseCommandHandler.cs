using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.StockPurchase.Commands.Submit;

public class SubmitStockPurchaseCommandHandler
    : IRequestHandler<SubmitStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;

    public SubmitStockPurchaseCommandHandler(IStockPurchaseRepository purchaseRepository)
    {
        _purchaseRepository = purchaseRepository;
    }

    public async Task<BaseResponse> Handle(SubmitStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null)
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Draft)
            return new BaseResponse { Success = false, Message = "Yalnız draft statusundakı alışlar göndərilə bilər." };

        purchase.Status = StockPurchaseStatus.Pending;
        _purchaseRepository.Update(purchase);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse { Success = true, Message = "Stok alışı təsdiq üçün göndərildi." };
    }
}

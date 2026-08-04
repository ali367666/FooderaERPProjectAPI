using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.StockPurchase.Commands.Delete;

public class DeleteStockPurchaseCommandHandler
    : IRequestHandler<DeleteStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;

    public DeleteStockPurchaseCommandHandler(IStockPurchaseRepository purchaseRepository)
    {
        _purchaseRepository = purchaseRepository;
    }

    public async Task<BaseResponse> Handle(DeleteStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null)
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Draft)
            return new BaseResponse { Success = false, Message = "Yalnız draft statusundakı alışlar silinə bilər." };

        _purchaseRepository.Remove(purchase);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse { Success = true, Message = "Stok alışı silindi." };
    }
}

using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.StockPurchase.Commands.Delete;

public class DeleteStockPurchaseCommandHandler
    : IRequestHandler<DeleteStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteStockPurchaseCommandHandler(IStockPurchaseRepository purchaseRepository, ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
        _purchaseRepository = purchaseRepository;
    }

    public async Task<BaseResponse> Handle(DeleteStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null || !_currentUserService.CanAccessCompany(purchase.CompanyId))
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Draft)
            return new BaseResponse { Success = false, Message = "Yalnız draft statusundakı alışlar silinə bilər." };

        _purchaseRepository.Remove(purchase);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse { Success = true, Message = "Stok alışı silindi." };
    }
}

using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.StockPurchase.Commands.Approve;

public class ApproveStockPurchaseCommandHandler
    : IRequestHandler<ApproveStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;

    public ApproveStockPurchaseCommandHandler(
        IStockPurchaseRepository purchaseRepository,
        IWarehouseStockRepository warehouseStockRepository)
    {
        _purchaseRepository = purchaseRepository;
        _warehouseStockRepository = warehouseStockRepository;
    }

    public async Task<BaseResponse> Handle(ApproveStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null)
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Pending)
            return new BaseResponse { Success = false, Message = "Yalnız gözləmədəki alışlar təsdiq edilə bilər." };

        // Anbar stokunu artır
        foreach (var line in purchase.Lines)
        {
            var stock = await _warehouseStockRepository.GetOrCreateZeroBalanceAsync(
                purchase.CompanyId,
                purchase.WarehouseId,
                line.StockItemId,
                unitId: 0,
                createdByUserId: null,
                utcNow: DateTime.UtcNow,
                cancellationToken);

            stock.Quantity += line.Quantity;
            _warehouseStockRepository.Update(stock);
        }

        purchase.Status = StockPurchaseStatus.Completed;
        _purchaseRepository.Update(purchase);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse { Success = true, Message = "Stok alışı təsdiqləndi və anbar yeniləndi." };
    }
}

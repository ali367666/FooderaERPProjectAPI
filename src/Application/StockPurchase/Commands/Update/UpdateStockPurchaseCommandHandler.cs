using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using Domain.Entities.WarehouseAndStock;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockPurchase.Commands.Update;

public class UpdateStockPurchaseCommandHandler
    : IRequestHandler<UpdateStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateStockPurchaseCommandHandler> _logger;

    public UpdateStockPurchaseCommandHandler(
        IStockPurchaseRepository purchaseRepository,
        IAuditLogService auditLogService,
        ILogger<UpdateStockPurchaseCommandHandler> logger)
    {
        _purchaseRepository = purchaseRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(UpdateStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null)
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Draft)
            return new BaseResponse { Success = false, Message = "Yalnız draft statusundakı alışlar dəyişdirilə bilər." };

        var dto = request.Request;

        if (dto.ExchangeRate <= 0)
            return new BaseResponse { Success = false, Message = "Məzənnə 0-dan böyük olmalıdır." };

        purchase.CounterpartyId = dto.CounterpartyId;
        purchase.IsImport = dto.IsImport;
        purchase.Currency = dto.Currency;
        purchase.ExchangeRate = dto.ExchangeRate;
        purchase.PurchaseDate = dto.PurchaseDate;
        purchase.WarehouseId = dto.WarehouseId;
        purchase.Note = dto.Note;

        purchase.Lines.Clear();
        foreach (var l in dto.Lines)
        {
            purchase.Lines.Add(new StockPurchaseLine
            {
                StockItemId = l.StockItemId,
                Quantity = l.Quantity,
                UnitPriceForeign = l.UnitPriceForeign,
                UnitPriceAzn = l.UnitPriceForeign * dto.ExchangeRate,
            });
        }

        _purchaseRepository.Update(purchase);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "StockPurchase",
                EntityId = purchase.Id.ToString(),
                ActionType = "Update",
                Message = $"StockPurchase yeniləndi. Id: {purchase.Id}",
                IsSuccess = true
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockPurchase update audit log xətası. Id: {Id}", purchase.Id);
        }

        return new BaseResponse { Success = true, Message = "Stok alışı uğurla yeniləndi." };
    }
}

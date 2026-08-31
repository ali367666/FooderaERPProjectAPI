using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockPurchase.Commands.Create;

public class CreateStockPurchaseCommandHandler
    : IRequestHandler<CreateStockPurchaseCommand, BaseResponse<int>>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateStockPurchaseCommandHandler> _logger;

    public CreateStockPurchaseCommandHandler(
        IStockPurchaseRepository purchaseRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateStockPurchaseCommandHandler> logger)
    {
        _purchaseRepository = purchaseRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateStockPurchaseCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;
        var companyId = _currentUserService.CompanyId;

        if (companyId == 0)
            return new BaseResponse<int> { Success = false, Message = "CompanyId tapılmadı." };

        if (dto.Lines is null || !dto.Lines.Any())
            return new BaseResponse<int> { Success = false, Message = "Alış ən azı bir sətir içərməlidir." };

        if (dto.Lines.Any(x => x.Quantity <= 0))
            return new BaseResponse<int> { Success = false, Message = "Bütün miqdarlar 0-dan böyük olmalıdır." };

        if (dto.Lines.Any(x => x.UnitPriceForeign < 0))
            return new BaseResponse<int> { Success = false, Message = "Vahid qiymət mənfi ola bilməz." };

        if (dto.ExchangeRate <= 0)
            return new BaseResponse<int> { Success = false, Message = "Məzənnə 0-dan böyük olmalıdır." };

        var purchase = new Domain.Entities.WarehouseAndStock.StockPurchase
        {
            CompanyId = companyId,
            DocumentNo = Guid.NewGuid().ToString("N"),
            CounterpartyId = dto.CounterpartyId,
            IsImport = dto.IsImport,
            Currency = dto.Currency,
            ExchangeRate = dto.ExchangeRate,
            PurchaseDate = dto.PurchaseDate,
            WarehouseId = dto.WarehouseId,
            Note = dto.Note,
            Status = StockPurchaseStatus.Draft,
            Lines = dto.Lines.Select(l => new Domain.Entities.WarehouseAndStock.StockPurchaseLine
            {
                StockItemId = l.StockItemId,
                Quantity = l.Quantity,
                UnitPriceForeign = l.UnitPriceForeign,
                UnitPriceAzn = l.UnitPriceForeign * dto.ExchangeRate,
            }).ToList()
        };

        await _purchaseRepository.AddAsync(purchase, cancellationToken);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        purchase.DocumentNo = $"SP-{purchase.Id:D6}";
        _purchaseRepository.Update(purchase);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "StockPurchase",
                EntityId = purchase.Id.ToString(),
                ActionType = "Create",
                Message = $"StockPurchase yaradıldı. Id: {purchase.Id}, CounterpartyId: {purchase.CounterpartyId}, Currency: {purchase.Currency}, Rate: {purchase.ExchangeRate}",
                IsSuccess = true
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockPurchase audit log yazılarkən xəta. Id: {Id}", purchase.Id);
        }

        _logger.LogInformation("StockPurchase yaradıldı. Id: {Id}", purchase.Id);

        return new BaseResponse<int> { Success = true, Message = "Stok alışı uğurla yaradıldı.", Data = purchase.Id };
    }
}

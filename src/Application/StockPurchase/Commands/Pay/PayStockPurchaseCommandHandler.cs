using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockPurchase.Commands.Pay;

public class PayStockPurchaseCommandHandler : IRequestHandler<PayStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PayStockPurchaseCommandHandler> _logger;

    public PayStockPurchaseCommandHandler(
        IStockPurchaseRepository purchaseRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<PayStockPurchaseCommandHandler> logger)
    {
        _purchaseRepository = purchaseRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(PayStockPurchaseCommand command, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(command.Id, cancellationToken);

        if (purchase is null || !_currentUserService.CanAccessCompany(purchase.CompanyId))
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Completed)
            return new BaseResponse { Success = false, Message = "Ödəniş yalnız təsdiqlənmiş alışa yazıla bilər." };

        var amount = Math.Round(command.Request.Amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
            return new BaseResponse { Success = false, Message = "Ödəniş məbləği 0-dan böyük olmalıdır." };

        var total = Math.Round(purchase.Lines.Sum(l => l.Quantity * l.UnitPriceAzn), 2, MidpointRounding.AwayFromZero);
        var remaining = Math.Max(0, total - purchase.PaidAmountAzn);
        if (amount > remaining)
            return new BaseResponse
            {
                Success = false,
                Message = $"Ödəniş qalan borcdan ({remaining:0.00} AZN) çox ola bilməz."
            };

        purchase.PaidAmountAzn += amount;
        purchase.LastModifiedByUserId = _currentUserService.UserId;
        purchase.LastModifiedAtUtc = DateTime.UtcNow;
        _purchaseRepository.Update(purchase);

        // The paid amount is a concurrency token: two payments at once cannot overwrite each other.
        if (!await _purchaseRepository.TrySaveChangesAsync(cancellationToken))
            return new BaseResponse
            {
                Success = false,
                Message = "Bu alış artıq başqa istifadəçi tərəfindən dəyişdirilib. Səhifəni yeniləyin."
            };

        try
        {
            var note = string.IsNullOrWhiteSpace(command.Request.Note) ? "" : $", Qeyd: {command.Request.Note.Trim()}";
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "StockPurchase",
                EntityId = purchase.Id.ToString(),
                ActionType = "Pay",
                Message = $"Təchizatçıya ödəniş: {amount:0.00} AZN. Alış: {purchase.DocumentNo}, ödənilmiş: {purchase.PaidAmountAzn:0.00}/{total:0.00}{note}",
                IsSuccess = true
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockPurchase pay audit log xətası. Id: {Id}", purchase.Id);
        }

        var left = Math.Max(0, total - purchase.PaidAmountAzn);
        return new BaseResponse
        {
            Success = true,
            Message = left == 0 ? "Ödəniş yazıldı. Borc tam bağlandı." : $"Ödəniş yazıldı. Qalan borc: {left:0.00} AZN."
        };
    }
}

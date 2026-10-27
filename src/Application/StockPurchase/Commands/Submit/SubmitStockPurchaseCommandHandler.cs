using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.StockPurchase.Commands.Submit;

public class SubmitStockPurchaseCommandHandler
    : IRequestHandler<SubmitStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public SubmitStockPurchaseCommandHandler(
        IStockPurchaseRepository purchaseRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService)
    {
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _purchaseRepository = purchaseRepository;
    }

    public async Task<BaseResponse> Handle(SubmitStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null || !_currentUserService.CanAccessCompany(purchase.CompanyId))
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Draft)
            return new BaseResponse { Success = false, Message = "Yalnız draft statusundakı alışlar göndərilə bilər." };

        purchase.Status = StockPurchaseStatus.Pending;
        purchase.LastModifiedByUserId = _currentUserService.UserId;
        purchase.LastModifiedAtUtc = DateTime.UtcNow;
        _purchaseRepository.Update(purchase);
        await _purchaseRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "StockPurchase",
                EntityId = purchase.Id.ToString(),
                ActionType = "Submit",
                Message = $"StockPurchase {purchase.DocumentNo}: Submit. UserId: {_currentUserService.UserId}",
                IsSuccess = true
            }, cancellationToken);
        }
        catch
        {
            // audit log failures must not block the operation
        }

        return new BaseResponse { Success = true, Message = "Stok alışı təsdiq üçün göndərildi." };
    }
}

using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseStock.Commands.PosAdjust;

public class PosAdjustWarehouseStockCommandHandler : IRequestHandler<PosAdjustWarehouseStockCommand, BaseResponse>
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public PosAdjustWarehouseStockCommandHandler(
        IWarehouseStockRepository warehouseStockRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<BaseResponse> Handle(PosAdjustWarehouseStockCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        if (dto.NewQuantity < 0)
            return BaseResponse.Fail("Miqdar mənfi ola bilməz.");

        var row = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
            companyId, dto.WarehouseId, dto.StockItemId, cancellationToken);

        var oldQuantity = row?.Quantity ?? 0;

        if (row is null)
        {
            row = await _warehouseStockRepository.GetOrCreateZeroBalanceAsync(
                companyId, dto.WarehouseId, dto.StockItemId, dto.UnitId, _currentUserService.UserId, DateTime.UtcNow, cancellationToken);
        }

        row.Quantity = dto.NewQuantity;
        _warehouseStockRepository.Update(row);
        await _warehouseStockRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseStock",
                    EntityId = row.Id.ToString(),
                    ActionType = "PosAdjust",
                    Message = $"POS-dan anbar düzəlişi: WarehouseId {dto.WarehouseId}, StockItemId {dto.StockItemId}, {oldQuantity} -> {dto.NewQuantity}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch
        {
            // audit log failures must not block the operation
        }

        return BaseResponse.Ok("Anbar miqdarı yeniləndi.");
    }
}

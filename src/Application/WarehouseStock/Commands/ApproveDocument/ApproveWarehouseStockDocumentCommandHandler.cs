using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using StockBalanceRow = Domain.Entities.WarehouseAndStock.WarehouseStock;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Commands.ApproveDocument;

public class ApproveWarehouseStockDocumentCommandHandler
    : IRequestHandler<ApproveWarehouseStockDocumentCommand, BaseResponse>
{
    private readonly IWarehouseStockDocumentRepository _documentRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ApproveWarehouseStockDocumentCommandHandler> _logger;

    public ApproveWarehouseStockDocumentCommandHandler(
        IWarehouseStockDocumentRepository documentRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<ApproveWarehouseStockDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        ApproveWarehouseStockDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdWithLinesAsync(request.Id, cancellationToken);
        if (document is null)
        {
            return BaseResponse.Fail("Warehouse stock document not found.");
        }

        if (document.Status != WarehouseStockDocumentStatus.Draft)
        {
            return BaseResponse.Fail("Only draft documents can be approved.");
        }

        if (document.Lines is null || document.Lines.Count == 0)
        {
            return BaseResponse.Fail("Document has no lines.");
        }

        var userId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in document.Lines)
            {
                var stock = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                    document.CompanyId,
                    document.WarehouseId,
                    line.StockItemId,
                    cancellationToken);

                if (stock is null)
                {
                    await _warehouseStockRepository.AddAsync(
                        new StockBalanceRow
                        {
                            CompanyId = document.CompanyId,
                            WarehouseId = document.WarehouseId,
                            StockItemId = line.StockItemId,
                            Quantity = line.Quantity,
                            UnitId = line.UnitId,
                            CreatedAtUtc = now,
                            CreatedByUserId = userId,
                        },
                        cancellationToken);
                }
                else
                {
                    stock.Quantity += line.Quantity;
                    stock.UnitId = line.UnitId;
                    stock.LastModifiedAtUtc = now;
                    stock.LastModifiedByUserId = userId;
                    _warehouseStockRepository.Update(stock);
                }

                await _stockMovementRepository.AddAsync(
                    new StockMovement
                    {
                        CompanyId = document.CompanyId,
                        WarehouseId = document.WarehouseId,
                        FromWarehouseId = null,
                        ToWarehouseId = document.WarehouseId,
                        StockItemId = line.StockItemId,
                        Type = StockMovementType.StockEntryIn,
                        SourceType = StockMovementSourceType.StockEntryDocument,
                        SourceId = document.Id,
                        SourceDocumentNo = document.DocumentNo,
                        MovementDate = now,
                        Quantity = line.Quantity,
                        WarehouseTransferId = null,
                        Note = "Warehouse stock document approved",
                    },
                    cancellationToken);
            }

            document.Status = WarehouseStockDocumentStatus.Approved;
            document.LastModifiedAtUtc = now;
            document.LastModifiedByUserId = userId;
            _documentRepository.Update(document);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "ApproveWarehouseStockDocument failed. Id: {Id}", request.Id);
            throw;
        }

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            EntityName = "WarehouseStockDocument",
            EntityId = document.Id.ToString(),
            ActionType = "Approve",
            NewValues = AuditLogJsonHelper.ToJson(new { document.DocumentNo, document.Status }),
            Message = "Warehouse stock document approved; balances and stock movements recorded.",
            CompanyId = document.CompanyId,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("ApproveWarehouseStockDocument completed. Id: {Id}", document.Id);

        return BaseResponse.Ok("Document approved and stock balances updated.");
    }
}

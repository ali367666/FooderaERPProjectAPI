using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Commands.UpdateDocument;

public class UpdateWarehouseStockDocumentCommandHandler
    : IRequestHandler<UpdateWarehouseStockDocumentCommand, BaseResponse>
{
    private readonly IWarehouseStockDocumentRepository _documentRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateWarehouseStockDocumentCommandHandler> _logger;

    public UpdateWarehouseStockDocumentCommandHandler(
        IWarehouseStockDocumentRepository documentRepository,
        IWarehouseRepository warehouseRepository,
        IStockItemRepository stockItemRepository,
        IAuditLogService auditLogService,
        ILogger<UpdateWarehouseStockDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _warehouseRepository = warehouseRepository;
        _stockItemRepository = stockItemRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdateWarehouseStockDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;

        var document = await _documentRepository.GetByIdWithLinesAsync(request.Id, cancellationToken);
        if (document is null)
        {
            return BaseResponse.Fail("Warehouse stock document not found.");
        }

        if (document.Status != WarehouseStockDocumentStatus.Draft)
        {
            return BaseResponse.Fail("Only draft documents can be updated.");
        }

        var warehouse = await _warehouseRepository.GetByIdAsync(req.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return BaseResponse.Fail("Warehouse not found.");
        }

        if (warehouse.CompanyId != document.CompanyId)
        {
            return BaseResponse.Fail("Warehouse does not belong to the same company as this document.");
        }

        if (req.Lines is null || req.Lines.Count == 0)
        {
            return BaseResponse.Fail("At least one line is required.");
        }

        var stockItemIds = req.Lines.Select(l => l.StockItemId).ToList();
        if (stockItemIds.Count != stockItemIds.Distinct().Count())
        {
            return BaseResponse.Fail("Duplicate stock items are not allowed on the same document.");
        }

        foreach (var line in req.Lines)
        {
            if (line.Quantity <= 0)
            {
                return BaseResponse.Fail("Each line must have quantity greater than zero.");
            }

            if (!Enum.IsDefined(typeof(UnitOfMeasure), line.UnitId))
            {
                return BaseResponse.Fail("Invalid unit of measure.");
            }

            var item = await _stockItemRepository.GetByIdAsync(line.StockItemId, cancellationToken);
            if (item is null)
            {
                return BaseResponse.Fail($"Stock item not found (Id: {line.StockItemId}).");
            }

            if (item.CompanyId != warehouse.CompanyId)
            {
                return BaseResponse.Fail("Warehouse and stock items must belong to the same company.");
            }
        }

        document.WarehouseId = warehouse.Id;

        foreach (var line in document.Lines.ToList())
        {
            document.Lines.Remove(line);
        }

        foreach (var l in req.Lines)
        {
            document.Lines.Add(new WarehouseStockLine
            {
                CompanyId = document.CompanyId,
                WarehouseStockDocumentId = document.Id,
                StockItemId = l.StockItemId,
                Quantity = l.Quantity,
                UnitId = l.UnitId
            });
        }

        _documentRepository.Update(document);
        await _documentRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            EntityName = "WarehouseStockDocument",
            EntityId = document.Id.ToString(),
            ActionType = "Update",
            NewValues = AuditLogJsonHelper.ToJson(new
            {
                document.WarehouseId,
                LineCount = document.Lines.Count
            }),
            Message = "Warehouse stock document updated.",
            CompanyId = document.CompanyId,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("UpdateWarehouseStockDocument completed. Id: {Id}", document.Id);

        return BaseResponse.Ok("Warehouse stock document updated successfully.");
    }
}

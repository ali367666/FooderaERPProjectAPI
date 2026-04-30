using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Commands.CreateDocument;

public class CreateWarehouseStockDocumentCommandHandler
    : IRequestHandler<CreateWarehouseStockDocumentCommand, BaseResponse<int>>
{
    private readonly IWarehouseStockDocumentRepository _documentRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateWarehouseStockDocumentCommandHandler> _logger;

    public CreateWarehouseStockDocumentCommandHandler(
        IWarehouseStockDocumentRepository documentRepository,
        IWarehouseRepository warehouseRepository,
        IStockItemRepository stockItemRepository,
        IAuditLogService auditLogService,
        ILogger<CreateWarehouseStockDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _warehouseRepository = warehouseRepository;
        _stockItemRepository = stockItemRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateWarehouseStockDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;

        _logger.LogInformation(
            "CreateWarehouseStockDocument started. WarehouseId: {WarehouseId}, LineCount: {LineCount}",
            req.WarehouseId,
            req.Lines?.Count ?? 0);

        var warehouse = await _warehouseRepository.GetByIdAsync(req.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            _logger.LogWarning("Warehouse not found. WarehouseId: {WarehouseId}", req.WarehouseId);
            return BaseResponse<int>.Fail("Warehouse not found.");
        }

        if (req.Lines is null || req.Lines.Count == 0)
        {
            return BaseResponse<int>.Fail("At least one line is required.");
        }

        var stockItemIds = req.Lines.Select(l => l.StockItemId).ToList();
        if (stockItemIds.Count != stockItemIds.Distinct().Count())
        {
            return BaseResponse<int>.Fail("Duplicate stock items are not allowed on the same document.");
        }

        foreach (var line in req.Lines)
        {
            if (line.Quantity <= 0)
            {
                return BaseResponse<int>.Fail("Each line must have quantity greater than zero.");
            }

            if (!Enum.IsDefined(typeof(UnitOfMeasure), line.UnitId))
            {
                return BaseResponse<int>.Fail("Invalid unit of measure.");
            }

            var item = await _stockItemRepository.GetByIdAsync(line.StockItemId, cancellationToken);
            if (item is null)
            {
                return BaseResponse<int>.Fail($"Stock item not found (Id: {line.StockItemId}).");
            }

            if (item.CompanyId != warehouse.CompanyId)
            {
                return BaseResponse<int>.Fail("Warehouse and stock items must belong to the same company.");
            }
        }

        var tempDocNo = Guid.NewGuid().ToString("N");
        var document = new WarehouseStockDocument
        {
            CompanyId = warehouse.CompanyId,
            WarehouseId = warehouse.Id,
            DocumentNo = tempDocNo,
            Status = WarehouseStockDocumentStatus.Draft,
            Lines = req.Lines.Select(l => new WarehouseStockLine
            {
                CompanyId = warehouse.CompanyId,
                StockItemId = l.StockItemId,
                Quantity = l.Quantity,
                UnitId = l.UnitId
            }).ToList()
        };

        await _documentRepository.AddAsync(document, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);

        document.DocumentNo = $"WSD-{document.Id:D6}";
        await _documentRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            EntityName = "WarehouseStockDocument",
            EntityId = document.Id.ToString(),
            ActionType = "Create",
            NewValues = AuditLogJsonHelper.ToJson(new
            {
                document.DocumentNo,
                document.WarehouseId,
                LineCount = document.Lines.Count
            }),
            Message = "Warehouse stock document created.",
            CompanyId = document.CompanyId,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation(
            "CreateWarehouseStockDocument completed. Id: {Id}, DocumentNo: {DocumentNo}",
            document.Id,
            document.DocumentNo);

        return BaseResponse<int>.Ok(document.Id, "Warehouse stock document created successfully.");
    }
}

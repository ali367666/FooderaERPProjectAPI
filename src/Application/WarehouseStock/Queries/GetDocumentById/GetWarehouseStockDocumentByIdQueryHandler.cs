using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Queries.GetDocumentById;

public class GetWarehouseStockDocumentByIdQueryHandler
    : IRequestHandler<GetWarehouseStockDocumentByIdQuery, BaseResponse<WarehouseStockDocumentDetailResponse>>
{
    private readonly IWarehouseStockDocumentRepository _documentRepository;
    private readonly ILogger<GetWarehouseStockDocumentByIdQueryHandler> _logger;

    public GetWarehouseStockDocumentByIdQueryHandler(
        IWarehouseStockDocumentRepository documentRepository,
        ILogger<GetWarehouseStockDocumentByIdQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<WarehouseStockDocumentDetailResponse>> Handle(
        GetWarehouseStockDocumentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (document is null)
        {
            _logger.LogWarning("Warehouse stock document not found. Id: {Id}", request.Id);
            return BaseResponse<WarehouseStockDocumentDetailResponse>.Fail("Warehouse stock document not found.");
        }

        var response = new WarehouseStockDocumentDetailResponse
        {
            Id = document.Id,
            DocumentNo = document.DocumentNo,
            WarehouseId = document.WarehouseId,
            WarehouseName = document.Warehouse?.Name ?? string.Empty,
            CompanyId = document.CompanyId,
            CreatedAtUtc = document.CreatedAtUtc,
            Status = (int)document.Status,
            Lines = document.Lines
                .OrderBy(l => l.Id)
                .Select(l => new WarehouseStockDocumentLineResponse
                {
                    Id = l.Id,
                    StockItemId = l.StockItemId,
                    StockItemName = l.StockItem?.Name ?? string.Empty,
                    Quantity = l.Quantity,
                    UnitId = l.UnitId
                })
                .ToList()
        };

        return BaseResponse<WarehouseStockDocumentDetailResponse>.Ok(response);
    }
}

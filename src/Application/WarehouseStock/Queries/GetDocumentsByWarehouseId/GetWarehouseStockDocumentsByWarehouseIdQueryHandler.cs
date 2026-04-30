using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Queries.GetDocumentsByWarehouseId;

public class GetWarehouseStockDocumentsByWarehouseIdQueryHandler
    : IRequestHandler<GetWarehouseStockDocumentsByWarehouseIdQuery, BaseResponse<List<WarehouseStockDocumentSummaryResponse>>>
{
    private readonly IWarehouseStockDocumentRepository _documentRepository;
    private readonly ILogger<GetWarehouseStockDocumentsByWarehouseIdQueryHandler> _logger;

    public GetWarehouseStockDocumentsByWarehouseIdQueryHandler(
        IWarehouseStockDocumentRepository documentRepository,
        ILogger<GetWarehouseStockDocumentsByWarehouseIdQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseStockDocumentSummaryResponse>>> Handle(
        GetWarehouseStockDocumentsByWarehouseIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Get warehouse stock documents by warehouse. WarehouseId: {WarehouseId}",
            request.WarehouseId);

        var documents = await _documentRepository.GetByWarehouseIdAsync(request.WarehouseId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            documents = documents
                .Where(x =>
                    x.DocumentNo.ToLower().Contains(search) ||
                    x.Id.ToString().Contains(search) ||
                    x.Lines.Any(l =>
                        (l.StockItem?.Name ?? "").ToLower().Contains(search)))
                .ToList();
        }

        var response = documents.Select(x => new WarehouseStockDocumentSummaryResponse
        {
            Id = x.Id,
            DocumentNo = x.DocumentNo,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.Warehouse?.Name ?? string.Empty,
            CompanyId = x.CompanyId,
            CreatedAtUtc = x.CreatedAtUtc,
            Status = (int)x.Status,
            LineCount = x.Lines?.Count ?? 0
        }).ToList();

        return BaseResponse<List<WarehouseStockDocumentSummaryResponse>>.Ok(response);
    }
}

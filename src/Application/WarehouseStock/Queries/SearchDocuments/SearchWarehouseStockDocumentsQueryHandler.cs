using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Queries.SearchDocuments;

public class SearchWarehouseStockDocumentsQueryHandler
    : IRequestHandler<SearchWarehouseStockDocumentsQuery, BaseResponse<List<WarehouseStockDocumentSummaryResponse>>>
{
    private readonly IWarehouseStockDocumentRepository _documentRepository;
    private readonly ILogger<SearchWarehouseStockDocumentsQueryHandler> _logger;

    public SearchWarehouseStockDocumentsQueryHandler(
        IWarehouseStockDocumentRepository documentRepository,
        ILogger<SearchWarehouseStockDocumentsQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseStockDocumentSummaryResponse>>> Handle(
        SearchWarehouseStockDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Search warehouse stock documents. CompanyId: {CompanyId}, Search: {Search}",
            request.CompanyId,
            request.Search);

        var documents = await _documentRepository.SearchByCompanyAsync(
            request.CompanyId,
            request.Search,
            cancellationToken);

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

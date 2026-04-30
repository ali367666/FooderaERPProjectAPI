using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;

namespace Application.WarehouseStock.Queries.SearchDocuments;

public record SearchWarehouseStockDocumentsQuery(int CompanyId, string? Search)
    : IRequest<BaseResponse<List<WarehouseStockDocumentSummaryResponse>>>;

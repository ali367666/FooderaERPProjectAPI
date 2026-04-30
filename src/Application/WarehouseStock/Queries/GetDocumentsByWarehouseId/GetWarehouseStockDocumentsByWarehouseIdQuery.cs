using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;

namespace Application.WarehouseStock.Queries.GetDocumentsByWarehouseId;

public record GetWarehouseStockDocumentsByWarehouseIdQuery(int WarehouseId, string? Search)
    : IRequest<BaseResponse<List<WarehouseStockDocumentSummaryResponse>>>;

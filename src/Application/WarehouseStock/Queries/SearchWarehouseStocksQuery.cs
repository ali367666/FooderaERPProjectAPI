using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;

namespace Application.WarehouseStock.Queries.Search;

public record SearchWarehouseStocksQuery(int CompanyId, string? Search)
    : IRequest<BaseResponse<List<WarehouseStockResponse>>>;
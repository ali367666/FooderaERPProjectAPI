using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;

namespace Application.WarehouseStock.Queries.SearchBalances;

public record SearchWarehouseStockBalancesQuery(
    int CompanyId,
    int? WarehouseId,
    int? StockItemId,
    string? Search)
    : IRequest<BaseResponse<List<WarehouseStockBalanceResponse>>>;

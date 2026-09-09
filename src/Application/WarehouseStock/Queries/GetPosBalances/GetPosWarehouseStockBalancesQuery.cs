using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;

namespace Application.WarehouseStock.Queries.GetPosBalances;

public record GetPosWarehouseStockBalancesQuery(int RestaurantId, string? Search)
    : IRequest<BaseResponse<List<WarehouseStockBalanceResponse>>>;

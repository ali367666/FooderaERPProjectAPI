using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;

namespace Application.WarehouseStock.Queries.GetByWarehouseId;

public record GetWarehouseStocksByWarehouseIdQuery(int WarehouseId, string? Search)
    : IRequest<BaseResponse<List<WarehouseStockResponse>>>;
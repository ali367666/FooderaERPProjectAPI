using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;

namespace Application.WarehouseStock.Queries.GetById;

public record GetWarehouseStockByIdQuery(int Id)
    : IRequest<BaseResponse<WarehouseStockResponse>>;
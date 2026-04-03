using Application.Common.Responce;
using Application.WarehouseTransfers.Dtos.Response;
using MediatR;

namespace Application.WarehouseTransfer.Queries.GetById;

public record GetWarehouseTransferByIdQuery(int Id)
    : IRequest<BaseResponse<WarehouseTransferResponse>>;
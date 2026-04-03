using Application.Common.Responce;
using Application.WarehouseTransfers.Dtos.Response;
using MediatR;

namespace Application.WarehouseTransfer.Queries.GetAll;

public record GetAllWarehouseTransfersQuery()
    : IRequest<BaseResponse<List<WarehouseTransferResponse>>>;
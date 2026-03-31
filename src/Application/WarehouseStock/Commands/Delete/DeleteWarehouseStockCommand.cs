using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseStock.Commands.Delete;

public record DeleteWarehouseStockCommand(int Id) : IRequest<BaseResponse>;
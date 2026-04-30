using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Request;
using MediatR;

namespace Application.WarehouseStock.Commands.UpdateDocument;

public record UpdateWarehouseStockDocumentCommand(int Id, UpdateWarehouseStockDocumentRequest Request)
    : IRequest<BaseResponse>;

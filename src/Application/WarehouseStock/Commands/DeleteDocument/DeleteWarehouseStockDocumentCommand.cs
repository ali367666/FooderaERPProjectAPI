using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseStock.Commands.DeleteDocument;

public record DeleteWarehouseStockDocumentCommand(int Id)
    : IRequest<BaseResponse>;

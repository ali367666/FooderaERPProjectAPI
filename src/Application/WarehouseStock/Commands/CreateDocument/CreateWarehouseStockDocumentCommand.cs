using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Request;
using MediatR;

namespace Application.WarehouseStock.Commands.CreateDocument;

public record CreateWarehouseStockDocumentCommand(CreateWarehouseStockDocumentRequest Request)
    : IRequest<BaseResponse<int>>;

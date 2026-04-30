using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseStock.Commands.ApproveDocument;

public record ApproveWarehouseStockDocumentCommand(int Id)
    : IRequest<BaseResponse>;

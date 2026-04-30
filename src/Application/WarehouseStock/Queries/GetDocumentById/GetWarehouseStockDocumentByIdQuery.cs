using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;

namespace Application.WarehouseStock.Queries.GetDocumentById;

public record GetWarehouseStockDocumentByIdQuery(int Id)
    : IRequest<BaseResponse<WarehouseStockDocumentDetailResponse>>;

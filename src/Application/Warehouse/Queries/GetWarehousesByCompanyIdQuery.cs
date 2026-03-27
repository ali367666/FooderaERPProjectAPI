using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using MediatR;

namespace Application.Warehouse.Queries.GetByCompanyId;

public record GetWarehousesByCompanyIdQuery(int CompanyId)
    : IRequest<BaseResponse<List<WarehouseResponse>>>;
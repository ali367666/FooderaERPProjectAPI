using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using MediatR;

namespace Application.Warehouse.Queries.Search;

public record SearchWarehousesQuery(int CompanyId, string? Search)
    : IRequest<BaseResponse<List<WarehouseResponse>>>;
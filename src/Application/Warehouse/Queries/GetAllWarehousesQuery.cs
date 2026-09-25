using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using MediatR;

namespace Application.Warehouse.Queries.GetAll;

public record GetAllWarehousesQuery(int? CompanyId = null) : IRequest<BaseResponse<List<WarehouseResponse>>>;
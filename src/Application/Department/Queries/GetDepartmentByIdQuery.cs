using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;

namespace Application.Departments.Queries.GetById;

public record GetDepartmentByIdQuery(int Id, int CompanyId)
    : IRequest<BaseResponse<DepartmentResponse>>;
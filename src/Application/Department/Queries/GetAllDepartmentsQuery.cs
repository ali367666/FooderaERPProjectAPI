using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;

namespace Application.Departments.Queries.GetAll;

public record GetAllDepartmentsQuery(int CompanyId)
    : IRequest<BaseResponse<List<DepartmentResponse>>>;
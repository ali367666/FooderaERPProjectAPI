using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Queries.GetAll;

public record GetAllEmployeesQuery(int CompanyId) : IRequest<BaseResponse<List<EmployeeResponse>>>;
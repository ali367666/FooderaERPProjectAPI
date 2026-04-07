using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Queries.GetAll;

public record GetAllEmployeesQuery() : IRequest<BaseResponse<List<EmployeeResponse>>>;
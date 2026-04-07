using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Queries.GetById;

public record GetEmployeeByIdQuery(int Id) : IRequest<BaseResponse<EmployeeResponse>>;
using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Queries.GetEmployeesByPosition;

public record GetEmployeesByPositionQuery(int? PositionId, string? PositionName, int? CompanyId)
    : IRequest<BaseResponse<List<EmployeeResponse>>>;

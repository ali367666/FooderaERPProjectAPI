using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Commands.Create;

public record CreateEmployeeCommand(CreateEmployeeRequest Request)
    : IRequest<BaseResponse<int>>;
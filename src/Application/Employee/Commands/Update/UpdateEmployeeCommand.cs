using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Commands.Update;

public record UpdateEmployeeCommand(int Id, UpdateEmployeeRequest Request)
    : IRequest<BaseResponse>;
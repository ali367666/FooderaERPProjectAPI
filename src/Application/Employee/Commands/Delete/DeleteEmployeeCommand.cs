using Application.Common.Responce;
using MediatR;

namespace Application.Employees.Commands.Delete;

public record DeleteEmployeeCommand(int Id) : IRequest<BaseResponse>;
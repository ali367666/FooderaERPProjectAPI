using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;

namespace Application.Departments.Commands.Create;

public record CreateDepartmentCommand(CreateDepartmentRequest Request)
    : IRequest<BaseResponse<DepartmentResponse>>;
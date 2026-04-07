using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;

namespace Application.Departments.Commands.Update;

public record UpdateDepartmentCommand(int Id, int CompanyId, UpdateDepartmentRequest Request)
    : IRequest<BaseResponse<DepartmentResponse>>;
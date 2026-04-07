using Application.Common.Responce;
using MediatR;

namespace Application.Departments.Commands.Delete;

public record DeleteDepartmentCommand(int Id, int CompanyId)
    : IRequest<BaseResponse>;
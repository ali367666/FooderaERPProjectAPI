using Application.Common.Responce;
using Application.Workstation.Dtos.Request;
using Application.Workstation.Dtos.Responce;
using MediatR;

namespace Application.Workstation.Commands.Create;

public record CreateWorkstationCommand(CreateWorkstationRequest Request)
    : IRequest<BaseResponse<WorkstationResponse>>;

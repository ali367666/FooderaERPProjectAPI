using Application.Common.Responce;
using Application.Workstation.Dtos.Request;
using Application.Workstation.Dtos.Responce;
using MediatR;

namespace Application.Workstation.Commands.Update;

public record UpdateWorkstationCommand(int Id, UpdateWorkstationRequest Request)
    : IRequest<BaseResponse<WorkstationResponse>>;

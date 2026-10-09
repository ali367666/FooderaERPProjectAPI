using Application.Common.Responce;
using MediatR;

namespace Application.Workstation.Commands.Delete;

public record DeleteWorkstationCommand(int Id) : IRequest<BaseResponse>;

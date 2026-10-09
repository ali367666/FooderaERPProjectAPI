using Application.Common.Responce;
using Application.Workstation.Dtos.Responce;
using MediatR;

namespace Application.Workstation.Queries;

public record GetAllWorkstationsQuery(int? RestaurantId)
    : IRequest<BaseResponse<List<WorkstationResponse>>>;

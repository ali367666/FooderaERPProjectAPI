using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Position.Dtos;
using MediatR;

namespace Application.Positions.Queries.GetById;

public class GetPositionByIdQueryHandler
    : IRequestHandler<GetPositionByIdQuery, BaseResponse<PositionResponse>>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetPositionByIdQueryHandler(
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService)
    {
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<PositionResponse>> Handle(
        GetPositionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var position = await _positionRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (position is null)
        {
            return new BaseResponse<PositionResponse>
            {
                Success = false,
                Message = "Position not found."
            };
        }

        var response = new PositionResponse
        {
            Id = position.Id,
            DepartmentId = position.DepartmentId,
            Name = position.Name
        };

        return new BaseResponse<PositionResponse>
        {
            Success = true,
            Message = "Position retrieved successfully.",
            Data = response
        };
    }
}
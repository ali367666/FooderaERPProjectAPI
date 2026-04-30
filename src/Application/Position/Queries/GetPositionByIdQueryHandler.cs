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

        if (companyId <= 0)
        {
            return new BaseResponse<PositionResponse>
            {
                Success = false,
                Message = "Company context is required."
            };
        }

        var position = await _positionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (position is null || position.CompanyId != companyId)
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
            CompanyId = position.CompanyId,
            DepartmentId = position.DepartmentId,
            Name = position.Name,
            Description = position.Description,
            DepartmentName = position.Department?.Name,
            CompanyName = position.Company?.Name,
        };

        return new BaseResponse<PositionResponse>
        {
            Success = true,
            Message = "Position retrieved successfully.",
            Data = response
        };
    }
}
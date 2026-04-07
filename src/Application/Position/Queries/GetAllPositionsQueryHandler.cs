using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Position.Dtos;
using MediatR;

namespace Application.Positions.Queries.GetAll;

public class GetAllPositionsQueryHandler
    : IRequestHandler<GetAllPositionsQuery, BaseResponse<List<PositionResponse>>>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllPositionsQueryHandler(
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService)
    {
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<PositionResponse>>> Handle(
        GetAllPositionsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var positions = await _positionRepository.GetAllAsync(companyId, cancellationToken);

        var response = positions
            .Select(x => new PositionResponse
            {
                Id = x.Id,
                DepartmentId = x.DepartmentId,
                Name = x.Name
            })
            .ToList();

        return new BaseResponse<List<PositionResponse>>
        {
            Success = true,
            Message = "Positions retrieved successfully.",
            Data = response
        };
    }
}
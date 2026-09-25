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
        // A tenant Admin is always confined to their own company, regardless of what companyId was
        // requested — only SuperAdmin may target an arbitrary company.
        var companyId = _currentUserService.IsSuperAdmin ? request.CompanyId : _currentUserService.CompanyId;

        if (companyId <= 0)
        {
            return BaseResponse<List<PositionResponse>>.Fail("Valid companyId is required.");
        }

        var positions = await _positionRepository.GetAllAsync(companyId, cancellationToken);

        var response = positions
            .Select(x => new PositionResponse
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                DepartmentId = x.DepartmentId,
                Name = x.Name,
                Description = x.Description,
                DepartmentName = x.Department?.Name,
                CompanyName = x.Company?.Name,
            })
            .ToList();

        return BaseResponse<List<PositionResponse>>.Ok(
            response,
            "Positions retrieved successfully.");
    }
}
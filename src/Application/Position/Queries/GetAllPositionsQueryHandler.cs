using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Position.Dtos;
using MediatR;

namespace Application.Positions.Queries.GetAll;

public class GetAllPositionsQueryHandler
    : IRequestHandler<GetAllPositionsQuery, BaseResponse<List<PositionResponse>>>
{
    private readonly IPositionRepository _positionRepository;

    public GetAllPositionsQueryHandler(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<BaseResponse<List<PositionResponse>>> Handle(
        GetAllPositionsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CompanyId <= 0)
        {
            return BaseResponse<List<PositionResponse>>.Fail("Valid companyId is required.");
        }

        var positions = await _positionRepository.GetAllAsync(request.CompanyId, cancellationToken);

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
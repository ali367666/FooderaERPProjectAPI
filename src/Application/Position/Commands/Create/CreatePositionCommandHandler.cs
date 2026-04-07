using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces;
using Application.Common.Responce;
using Domain.Entities;
using MediatR;

namespace Application.Positions.Commands.Create;

public class CreatePositionCommandHandler
    : IRequestHandler<CreatePositionCommand, BaseResponse<int>>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreatePositionCommandHandler(
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService)
    {
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<int>> Handle(
        CreatePositionCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var exists = await _positionRepository.ExistsByNameAsync(
            companyId,
            request.Request.DepartmentId,
            request.Request.Name.Trim(),
            cancellationToken);

        if (exists)
        {
            return new BaseResponse<int>
            {
                Success = false,
                Message = "Position with this name already exists in the department."
            };
        }

        var position = new Domain.Entities.Position
        {
            CompanyId = companyId,
            DepartmentId = request.Request.DepartmentId,
            Name = request.Request.Name.Trim()
        };

        await _positionRepository.AddAsync(position, cancellationToken);
        await _positionRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse<int>
        {
            Success = true,
            Message = "Position created successfully.",
            Data = position.Id
        };
    }
}
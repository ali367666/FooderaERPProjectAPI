using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.Positions.Commands.Update;

public class UpdatePositionCommandHandler
    : IRequestHandler<UpdatePositionCommand, BaseResponse>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePositionCommandHandler(
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService)
    {
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse> Handle(
        UpdatePositionCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var position = await _positionRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (position is null)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "Position not found."
            };
        }

        var nameChanged = !string.Equals(
            position.Name,
            request.Request.Name.Trim(),
            StringComparison.OrdinalIgnoreCase);

        var departmentChanged = position.DepartmentId != request.Request.DepartmentId;

        if (nameChanged || departmentChanged)
        {
            var exists = await _positionRepository.ExistsByNameAsync(
                companyId,
                request.Request.DepartmentId,
                request.Request.Name.Trim(),
                cancellationToken);

            if (exists)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Position with this name already exists in the department."
                };
            }
        }

        position.DepartmentId = request.Request.DepartmentId;
        position.Name = request.Request.Name.Trim();

        _positionRepository.Update(position);
        await _positionRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse
        {
            Success = true,
            Message = "Position updated successfully."
        };
    }
}
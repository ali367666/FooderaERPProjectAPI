using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.Positions.Commands.Delete;

public class DeletePositionCommandHandler
    : IRequestHandler<DeletePositionCommand, BaseResponse>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeletePositionCommandHandler(
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService)
    {
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse> Handle(
        DeletePositionCommand request,
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

        var hasEmployees = await _positionRepository.HasAnyEmployeeAsync(
            request.Id,
            cancellationToken);

        if (hasEmployees)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "This position cannot be deleted because it is assigned to one or more employees."
            };
        }

        _positionRepository.Delete(position);
        await _positionRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse
        {
            Success = true,
            Message = "Position deleted successfully."
        };
    }
}
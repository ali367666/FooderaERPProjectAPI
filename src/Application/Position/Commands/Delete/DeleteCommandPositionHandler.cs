using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Positions.Commands.Delete;

public class DeletePositionCommandHandler
    : IRequestHandler<DeletePositionCommand, BaseResponse>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeletePositionCommandHandler> _logger;

    public DeletePositionCommandHandler(
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<DeletePositionCommandHandler> logger)
    {
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeletePositionCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "DeletePositionCommand başladı. PositionId: {PositionId}, CompanyId: {CompanyId}",
            request.Id,
            companyId);

        var position = await _positionRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (position is null)
        {
            _logger.LogWarning(
                "Position silinmədi. Position tapılmadı. PositionId: {PositionId}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

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
            _logger.LogWarning(
                "Position silinmədi. Position işçilərə təyin olunub. PositionId: {PositionId}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            return new BaseResponse
            {
                Success = false,
                Message = "This position cannot be deleted because it is assigned to one or more employees."
            };
        }

        _positionRepository.Delete(position);
        await _positionRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Position",
                    EntityId = position.Id.ToString(),
                    ActionType = "Delete",
                    Message = $"Position silindi. Id: {position.Id}, Name: {position.Name}, DepartmentId: {position.DepartmentId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "Position üçün audit log yazıldı. PositionId: {PositionId}",
                position.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Position delete audit log yazılarkən xəta baş verdi. PositionId: {PositionId}",
                position.Id);
        }

        _logger.LogInformation(
            "Position uğurla silindi. PositionId: {PositionId}, CompanyId: {CompanyId}",
            position.Id,
            companyId);

        return new BaseResponse
        {
            Success = true,
            Message = "Position deleted successfully."
        };
    }
}
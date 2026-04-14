using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Positions.Commands.Update;

public class UpdatePositionCommandHandler
    : IRequestHandler<UpdatePositionCommand, BaseResponse>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdatePositionCommandHandler> _logger;

    public UpdatePositionCommandHandler(
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<UpdatePositionCommandHandler> logger)
    {
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdatePositionCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "UpdatePositionCommand başladı. PositionId: {PositionId}, NewName: {NewName}, NewDepartmentId: {NewDepartmentId}, CompanyId: {CompanyId}",
            request.Id,
            request.Request.Name,
            request.Request.DepartmentId,
            companyId);

        var position = await _positionRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (position is null)
        {
            _logger.LogWarning(
                "Position yenilənmədi. Position tapılmadı. PositionId: {PositionId}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            return new BaseResponse
            {
                Success = false,
                Message = "Position not found."
            };
        }

        var oldName = position.Name;
        var oldDepartmentId = position.DepartmentId;

        var trimmedName = request.Request.Name.Trim();

        var nameChanged = !string.Equals(
            position.Name,
            trimmedName,
            StringComparison.OrdinalIgnoreCase);

        var departmentChanged = position.DepartmentId != request.Request.DepartmentId;

        if (nameChanged || departmentChanged)
        {
            var exists = await _positionRepository.ExistsByNameAsync(
                companyId,
                request.Request.DepartmentId,
                trimmedName,
                cancellationToken);

            if (exists)
            {
                _logger.LogWarning(
                    "Position yenilənmədi. Eyni adda position artıq mövcuddur. PositionId: {PositionId}, Name: {Name}, DepartmentId: {DepartmentId}, CompanyId: {CompanyId}",
                    request.Id,
                    trimmedName,
                    request.Request.DepartmentId,
                    companyId);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Position with this name already exists in the department."
                };
            }
        }

        position.DepartmentId = request.Request.DepartmentId;
        position.Name = trimmedName;

        _positionRepository.Update(position);
        await _positionRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Position",
                    EntityId = position.Id.ToString(),
                    ActionType = "Update",
                    Message = $"Position yeniləndi. Id: {position.Id}, OldName: {oldName}, NewName: {position.Name}, OldDepartmentId: {oldDepartmentId}, NewDepartmentId: {position.DepartmentId}",
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
                "Position update audit log yazılarkən xəta baş verdi. PositionId: {PositionId}",
                position.Id);
        }

        _logger.LogInformation(
            "Position uğurla yeniləndi. PositionId: {PositionId}, CompanyId: {CompanyId}",
            position.Id,
            companyId);

        return new BaseResponse
        {
            Success = true,
            Message = "Position updated successfully."
        };
    }
}
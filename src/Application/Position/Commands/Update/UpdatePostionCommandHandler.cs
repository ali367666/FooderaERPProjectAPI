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
            "Updating Position: requested Id={PositionId}, NewName={NewName}, NewDepartmentId={NewDepartmentId}, current user CompanyId={CompanyId}, current user UserId={UserId}",
            request.Id,
            request.Request.Name,
            request.Request.DepartmentId,
            companyId,
            _currentUserService.UserId);

        if (companyId <= 0)
        {
            _logger.LogWarning(
                "Update Position rejected: invalid or missing company context (CompanyId={CompanyId}).",
                companyId);

            return BaseResponse.Fail("Company context is required.");
        }

        var position = await _positionRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        _logger.LogInformation(
            "DEBUG Update -> RequestId={RequestId}, UserCompanyId={UserCompanyId}, FoundPositionId={FoundPositionId}",
            request.Id,
            companyId,
            position?.Id);

        if (position is null)
        {
            _logger.LogWarning(
                "Update Position: no row with Id={PositionId} for CompanyId={CompanyId}.",
                request.Id,
                companyId);

            return BaseResponse.Fail("Position not found.");
        }

        var departmentExists = await _positionRepository.DepartmentExistsAsync(
            request.Request.DepartmentId,
            companyId,
            cancellationToken);

        if (!departmentExists)
        {
            _logger.LogWarning(
                "Update Position rejected: DepartmentId={DepartmentId} does not belong to CompanyId={CompanyId}.",
                request.Request.DepartmentId,
                companyId);

            return BaseResponse.Fail("Department not found.");
        }

        var oldName = position.Name;
        var oldDepartmentId = position.DepartmentId;

        var trimmedName = request.Request.Name.Trim();

        var exists = await _positionRepository.ExistsByNameAsync(
            companyId,
            request.Request.DepartmentId,
            trimmedName,
            cancellationToken);

        if (exists &&
            !(string.Equals(oldName, trimmedName, StringComparison.OrdinalIgnoreCase)
              && oldDepartmentId == request.Request.DepartmentId))
        {
            _logger.LogWarning(
                "Position update rejected because duplicate exists. PositionId={PositionId}, Name={Name}, DepartmentId={DepartmentId}, CompanyId={CompanyId}",
                request.Id,
                trimmedName,
                request.Request.DepartmentId,
                companyId);

            return BaseResponse.Fail("Position with this name already exists in the department.");
        }

        position.Name = trimmedName;
        position.DepartmentId = request.Request.DepartmentId;
        position.Description = request.Request.Description;

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
                    Message = $"Position updated. Id: {position.Id}, OldName: {oldName}, NewName: {position.Name}, OldDepartmentId: {oldDepartmentId}, NewDepartmentId: {position.DepartmentId}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Error while writing audit log for Position update. PositionId={PositionId}",
                position.Id);
        }

        _logger.LogInformation(
            "Position updated successfully. PositionId={PositionId}, CompanyId={CompanyId}",
            position.Id,
            companyId);

        return BaseResponse.Ok("Position updated successfully.");
    }
}
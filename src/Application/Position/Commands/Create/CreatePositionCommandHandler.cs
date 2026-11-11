using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Positions.Commands.Create;

public class CreatePositionCommandHandler
    : IRequestHandler<CreatePositionCommand, BaseResponse<int>>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreatePositionCommandHandler> _logger;
    private readonly Application.Common.Helpers.DepartmentResolver _departmentResolver;

    public CreatePositionCommandHandler(
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreatePositionCommandHandler> logger,
        Application.Common.Helpers.DepartmentResolver departmentResolver)
    {
        _departmentResolver = departmentResolver;
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<int>> Handle(
        CreatePositionCommand request,
        CancellationToken cancellationToken)
    {
        // SuperAdmin may target any company via the form's Company field; a tenant Admin is
        // always confined to their own company regardless of what was submitted.
        var companyId = _currentUserService.IsSuperAdmin && request.Request.CompanyId > 0
            ? request.Request.CompanyId
            : _currentUserService.CompanyId;

        // Without the Departments module a position is put in the company's default department.
        var departmentId = await _departmentResolver.ForNewPositionAsync(companyId, request.Request.DepartmentId, cancellationToken);

        _logger.LogInformation(
            "CreatePositionCommand başladı. Name: {Name}, DepartmentId: {DepartmentId}, CompanyId: {CompanyId}",
            request.Request.Name,
            departmentId,
            companyId);

        var exists = await _positionRepository.ExistsByNameAsync(
            companyId,
            departmentId,
            request.Request.Name.Trim(),
            cancellationToken);

        if (exists)
        {
            _logger.LogWarning(
                "Position yaradılmadı. Eyni adda position artıq mövcuddur. Name: {Name}, DepartmentId: {DepartmentId}, CompanyId: {CompanyId}",
                request.Request.Name,
                departmentId,
                companyId);

            return new BaseResponse<int>
            {
                Success = false,
                Message = "Position with this name already exists in the department."
            };
        }

        var position = new Domain.Entities.Position
        {
            CompanyId = companyId,
            DepartmentId = departmentId,
            Name = request.Request.Name.Trim()
        };

        await _positionRepository.AddAsync(position, cancellationToken);
        await _positionRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Position",
                    EntityId = position.Id.ToString(),
                    ActionType = "Create",
                    Message = $"Position yaradıldı. Id: {position.Id}, Name: {position.Name}, DepartmentId: {position.DepartmentId}",
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
                "Position create audit log yazılarkən xəta baş verdi. PositionId: {PositionId}",
                position.Id);
        }

        _logger.LogInformation(
            "Position uğurla yaradıldı. PositionId: {PositionId}, CompanyId: {CompanyId}",
            position.Id,
            companyId);

        return new BaseResponse<int>
        {
            Success = true,
            Message = "Position created successfully.",
            Data = position.Id
        };
    }
}
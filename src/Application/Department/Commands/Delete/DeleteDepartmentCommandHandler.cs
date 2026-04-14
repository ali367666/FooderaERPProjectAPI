using System.Text.Json;
using Application.Abstractions.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Departments.Commands.Delete;

public sealed class DeleteDepartmentCommandHandler
    : IRequestHandler<DeleteDepartmentCommand, BaseResponse>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteDepartmentCommandHandler> _logger;

    public DeleteDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IAuditLogService auditLogService,
        ILogger<DeleteDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "DeleteDepartmentCommand başladı. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                request.CompanyId);

            var department = await _departmentRepository.GetByIdAsync(
                request.Id,
                request.CompanyId,
                cancellationToken);

            if (department is null)
            {
                _logger.LogWarning(
                    "Department tapılmadı. Delete icra olunmadı. Id: {Id}, CompanyId: {CompanyId}",
                    request.Id,
                    request.CompanyId);

                return BaseResponse.Fail("Department tapılmadı.");
            }

            var hasPositions = await _departmentRepository.HasAnyPositionAsync(
                request.Id,
                cancellationToken);

            if (hasPositions)
            {
                _logger.LogWarning(
                    "Department silinmədi. Position-lar mövcuddur. Id: {Id}",
                    request.Id);

                return BaseResponse.Fail("Bu department-ə bağlı position-lar var, silmək olmaz.");
            }

            var hasEmployees = await _departmentRepository.HasAnyEmployeeAsync(
                request.Id,
                cancellationToken);

            if (hasEmployees)
            {
                _logger.LogWarning(
                    "Department silinmədi. Employee-lər mövcuddur. Id: {Id}",
                    request.Id);

                return BaseResponse.Fail("Bu department-ə bağlı employee-lər var, silmək olmaz.");
            }

            var oldValues = JsonSerializer.Serialize(new
            {
                department.Id,
                department.CompanyId,
                department.Name,
                department.Description
            });

            _departmentRepository.Delete(department);
            await _departmentRepository.SaveChangesAsync(cancellationToken);

            try
            {
                await _auditLogService.LogAsync(
                    new AuditLogEntry
                    {
                        EntityName = "Department",
                        EntityId = department.Id.ToString(),
                        ActionType = "Delete",
                        OldValues = oldValues,
                        NewValues = null,
                        Message = $"Department silindi. Id: {department.Id}, Ad: {department.Name}, CompanyId: {department.CompanyId}",
                        IsSuccess = true
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Department üçün audit log yazıldı. Id: {Id}",
                    department.Id);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(
                    auditEx,
                    "Department delete audit log yazılarkən xəta baş verdi. Id: {Id}",
                    department.Id);
            }

            _logger.LogInformation(
                "Department uğurla silindi. Id: {Id}, CompanyId: {CompanyId}",
                department.Id,
                department.CompanyId);

            return BaseResponse.Ok("Department uğurla silindi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeleteDepartmentCommand zamanı xəta baş verdi. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                request.CompanyId);

            throw;
        }
    }
}
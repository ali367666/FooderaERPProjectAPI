using System.Text.Json;
using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Departments.Commands.Update;

public sealed class UpdateDepartmentCommandHandler
    : IRequestHandler<UpdateDepartmentCommand, BaseResponse<DepartmentResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateDepartmentCommandHandler> _logger;

    public UpdateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<UpdateDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<DepartmentResponse>> Handle(
        UpdateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var isSuperAdmin = _currentUserService.IsSuperAdmin;
            var callerCompanyId = _currentUserService.CompanyId;

            _logger.LogInformation(
                "UpdateDepartmentCommand başladı. Id: {Id}, RequestedCompanyId: {RequestedCompanyId}, CallerCompanyId: {CallerCompanyId}, IsSuperAdmin: {IsSuperAdmin}",
                request.Id,
                request.CompanyId,
                callerCompanyId,
                isSuperAdmin);

            if (string.IsNullOrWhiteSpace(request.Request.Name))
                return BaseResponse<DepartmentResponse>.Fail("Department adı boş ola bilməz.");

            // SuperAdmin may move a department to any company — look it up by id alone first, the
            // target company is resolved below. A tenant Admin is always confined to their own
            // company, regardless of what CompanyId was sent.
            var department = isSuperAdmin
                ? await _departmentRepository.GetByIdAsync(request.Id, cancellationToken)
                : await _departmentRepository.GetByIdAsync(request.Id, callerCompanyId, cancellationToken);

            if (department is null)
            {
                _logger.LogWarning(
                    "Department tapılmadı. Update icra olunmadı. Id: {Id}, CallerCompanyId: {CallerCompanyId}",
                    request.Id,
                    callerCompanyId);

                return BaseResponse<DepartmentResponse>.Fail("Department tapılmadı.");
            }

            var companyId = isSuperAdmin && request.CompanyId > 0
                ? request.CompanyId
                : (isSuperAdmin ? department.CompanyId : callerCompanyId);

            var trimmedName = request.Request.Name.Trim();

            if (!string.Equals(department.Name, trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _departmentRepository.ExistsByNameAsync(
                    companyId,
                    trimmedName,
                    cancellationToken);

                if (exists)
                {
                    _logger.LogWarning(
                        "Department update olunmadı. Duplicate name. Id: {Id}, CompanyId: {CompanyId}, Name: {Name}",
                        request.Id,
                        companyId,
                        trimmedName);

                    return BaseResponse<DepartmentResponse>.Fail("Bu adda department artıq mövcuddur.");
                }
            }

            var oldValues = JsonSerializer.Serialize(new
            {
                department.Id,
                department.CompanyId,
                department.Name,
                department.Description
            });

            department.Name = trimmedName;
            department.Description = request.Request.Description?.Trim();
            department.CompanyId = companyId;

            _departmentRepository.Update(department);
            await _departmentRepository.SaveChangesAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new
            {
                department.Id,
                department.CompanyId,
                department.Name,
                department.Description
            });

            try
            {
                await _auditLogService.LogAsync(
                    new AuditLogEntry
                    {
                        EntityName = "Department",
                        EntityId = department.Id.ToString(),
                        ActionType = "Update",
                        OldValues = oldValues,
                        NewValues = newValues,
                        Message = $"Department yeniləndi. Id: {department.Id}, Ad: {department.Name}, CompanyId: {department.CompanyId}",
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
                    "Department update audit log yazılarkən xəta baş verdi. Id: {Id}",
                    department.Id);
            }

            var response = new DepartmentResponse
            {
                Id = department.Id,
                CompanyId = department.CompanyId,
                Name = department.Name,
                Description = department.Description
            };

            _logger.LogInformation(
                "Department uğurla yeniləndi. Id: {Id}, CompanyId: {CompanyId}",
                department.Id,
                department.CompanyId);

            return BaseResponse<DepartmentResponse>.Ok(response, "Department uğurla yeniləndi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UpdateDepartmentCommand zamanı xəta baş verdi. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                request.CompanyId);

            throw;
        }
    }
}
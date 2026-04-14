using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Application.Departments.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Departments.Commands.Create;

public sealed class CreateDepartmentCommandHandler
    : IRequestHandler<CreateDepartmentCommand, BaseResponse<DepartmentResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateDepartmentCommandHandler> _logger;

    public CreateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<DepartmentResponse>> Handle(
        CreateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Request;
            var companyId = _currentUserService.CompanyId;

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BaseResponse<DepartmentResponse>.Fail("Department adı boş ola bilməz.");

            var departmentName = dto.Name.Trim();

            _logger.LogInformation(
                "CreateDepartmentCommand başladı. CompanyId: {CompanyId}, Name: {Name}",
                companyId,
                departmentName);

            var exists = await _departmentRepository.ExistsByNameAsync(
                companyId,
                departmentName,
                cancellationToken);

            if (exists)
            {
                _logger.LogWarning(
                    "Department yaradılmadı. Duplicate name. CompanyId: {CompanyId}, Name: {Name}",
                    companyId,
                    departmentName);

                return BaseResponse<DepartmentResponse>.Fail("Bu adda department artıq mövcuddur.");
            }

            var department = new Department
            {
                CompanyId = companyId,
                Name = departmentName,
                Description = string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim()
            };

            await _departmentRepository.AddAsync(department, cancellationToken);
            await _departmentRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Department DB-yə yazıldı. Id: {Id}",
                department.Id);

            // 🔥 AUDIT (SAFE şəkildə)
            try
            {
                await _auditLogService.LogAsync(
                    new AuditLogEntry
                    {
                        EntityName = "Department",
                        EntityId = department.Id.ToString(),
                        ActionType = "Create",
                        Message = $"Department yaradıldı. Id: {department.Id}, Ad: {department.Name}, CompanyId: {department.CompanyId}",
                        IsSuccess = true
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "AuditLog yazıldı. DepartmentId: {DepartmentId}",
                    department.Id);
            }
            catch (Exception auditEx)
            {
                // 🔥 Audit problemi əsas prosesi sındırmasın
                _logger.LogError(
                    auditEx,
                    "AuditLog yazılarkən xəta baş verdi. DepartmentId: {DepartmentId}",
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
                "Department uğurla yaradıldı. DepartmentId: {DepartmentId}, CompanyId: {CompanyId}",
                department.Id,
                department.CompanyId);

            return BaseResponse<DepartmentResponse>.Ok(response, "Department uğurla yaradıldı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CreateDepartmentCommand zamanı xəta baş verdi. Name: {Name}",
                request.Request.Name);

            throw;
        }
    }
}
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Employees.Commands.Delete;

public class DeleteEmployeeCommandHandler
    : IRequestHandler<DeleteEmployeeCommand, BaseResponse>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteEmployeeCommandHandler> _logger;

    public DeleteEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<DeleteEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var companyId = _currentUserService.CompanyId;

            _logger.LogInformation(
                "DeleteEmployeeCommand başladı. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            var employee = await _employeeRepository.GetByIdAsync(
                request.Id,
                companyId,
                cancellationToken);

            if (employee is null)
            {
                _logger.LogWarning(
                    "Employee silinmədi. Tapılmadı. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                    request.Id,
                    companyId);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Employee not found."
                };
            }

            var oldValues = JsonSerializer.Serialize(new
            {
                employee.Id,
                employee.CompanyId,
                employee.FirstName,
                employee.LastName,
                employee.FatherName,
                employee.PhoneNumber,
                employee.Email,
                employee.Address,
                employee.HireDate,
                employee.DepartmentId,
                employee.PositionId,
                employee.UserId,
                employee.IsActive
            });

            _employeeRepository.Delete(employee);
            await _employeeRepository.SaveChangesAsync(cancellationToken);

            try
            {
                await _auditLogService.LogAsync(
                    new AuditLogEntry
                    {
                        EntityName = "Employee",
                        EntityId = employee.Id.ToString(),
                        ActionType = "Delete",
                        OldValues = oldValues,
                        NewValues = null,
                        Message = $"Employee silindi. Id: {employee.Id}, AdSoyad: {employee.FirstName} {employee.LastName}, CompanyId: {employee.CompanyId}",
                        IsSuccess = true
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Employee üçün audit log yazıldı. EmployeeId: {EmployeeId}",
                    employee.Id);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(
                    auditEx,
                    "Employee delete audit log yazılarkən xəta baş verdi. EmployeeId: {EmployeeId}",
                    employee.Id);
            }

            _logger.LogInformation(
                "Employee uğurla silindi. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                employee.Id,
                employee.CompanyId);

            return new BaseResponse
            {
                Success = true,
                Message = "Employee deleted successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeleteEmployeeCommand zamanı xəta baş verdi. EmployeeId: {EmployeeId}",
                request.Id);

            throw;
        }
    }
}
using System.Text.Json;
using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Employees.Commands.Update;

public class UpdateEmployeeCommandHandler
    : IRequestHandler<UpdateEmployeeCommand, BaseResponse>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        IRestaurantRepository restaurantRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<UpdateEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _restaurantRepository = restaurantRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var isSuperAdmin = _currentUserService.IsSuperAdmin;
            var callerCompanyId = _currentUserService.CompanyId;

            _logger.LogInformation(
                "UpdateEmployeeCommand başladı. EmployeeId: {EmployeeId}, CallerCompanyId: {CallerCompanyId}, IsSuperAdmin: {IsSuperAdmin}",
                request.Id,
                callerCompanyId,
                isSuperAdmin);

            // SuperAdmin can move an employee to any company — look them up by id alone first, the
            // target company is resolved from whichever Department they're re-assigned to below. A
            // tenant Admin is always confined to their own company.
            var employee = isSuperAdmin
                ? await _employeeRepository.GetByIdAsync(request.Id, cancellationToken)
                : await _employeeRepository.GetByIdAsync(request.Id, callerCompanyId, cancellationToken);

            if (employee is null)
            {
                _logger.LogWarning(
                    "Employee update olunmadı. Employee tapılmadı. EmployeeId: {EmployeeId}, CallerCompanyId: {CallerCompanyId}",
                    request.Id,
                    callerCompanyId);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Employee not found."
                };
            }

            Domain.Entities.Department? department;
            int companyId;

            if (isSuperAdmin)
            {
                department = await _departmentRepository.GetByIdAsync(request.Request.DepartmentId, cancellationToken);
                companyId = department?.CompanyId ?? 0;
            }
            else
            {
                companyId = callerCompanyId;
                department = await _departmentRepository.GetByIdAsync(request.Request.DepartmentId, companyId, cancellationToken);
            }

            if (department is null)
            {
                _logger.LogWarning(
                    "Employee update olunmadı. Department tapılmadı. DepartmentId: {DepartmentId}, CallerCompanyId: {CallerCompanyId}",
                    request.Request.DepartmentId,
                    callerCompanyId);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Department not found."
                };
            }

            var position = await _positionRepository.GetByIdAsync(
                request.Request.PositionId,
                companyId,
                cancellationToken);

            if (position is null || position.DepartmentId != request.Request.DepartmentId)
            {
                _logger.LogWarning(
                    "Employee update olunmadı. Position yanlışdır. PositionId: {PositionId}, DepartmentId: {DepartmentId}, CompanyId: {CompanyId}",
                    request.Request.PositionId,
                    request.Request.DepartmentId,
                    companyId);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Invalid position."
                };
            }

            var duplicateContact = await _employeeRepository.ExistsByEmailOrPhoneAsync(
                companyId,
                request.Request.Email,
                request.Request.PhoneNumber,
                excludeEmployeeId: employee.Id,
                cancellationToken);

            if (duplicateContact)
            {
                _logger.LogWarning(
                    "Employee update olunmadı. Email/telefon artıq mövcuddur. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                    employee.Id,
                    companyId);

                return new BaseResponse
                {
                    Success = false,
                    Message = "An employee with this email or phone number already exists."
                };
            }

            var restaurant = await _restaurantRepository.GetByIdAsync(request.Request.RestaurantId, cancellationToken);

            if (restaurant is null || restaurant.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Employee update olunmadı. Restaurant tapılmadı və ya şirkətə aid deyil. RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                    request.Request.RestaurantId,
                    companyId);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Branch not found."
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

            employee.FirstName = request.Request.FirstName.Trim();
            employee.LastName = request.Request.LastName.Trim();
            employee.FatherName = request.Request.FatherName;

            employee.PhoneNumber = request.Request.PhoneNumber;
            employee.Email = request.Request.Email;
            employee.Address = request.Request.Address;

            employee.HireDate = request.Request.HireDate;

            employee.DepartmentId = request.Request.DepartmentId;
            employee.PositionId = request.Request.PositionId;
            employee.RestaurantId = restaurant.Id;
            employee.CompanyId = companyId;

            _employeeRepository.Update(employee);
            await _employeeRepository.SaveChangesAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new
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

            try
            {
                await _auditLogService.LogAsync(
                    new AuditLogEntry
                    {
                        EntityName = "Employee",
                        EntityId = employee.Id.ToString(),
                        ActionType = "Update",
                        OldValues = oldValues,
                        NewValues = newValues,
                        Message = $"Employee yeniləndi. Id: {employee.Id}, AdSoyad: {employee.FirstName} {employee.LastName}, DepartmentId: {employee.DepartmentId}, PositionId: {employee.PositionId}, CompanyId: {employee.CompanyId}",
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
                    "Employee update audit log yazılarkən xəta baş verdi. EmployeeId: {EmployeeId}",
                    employee.Id);
            }

            _logger.LogInformation(
                "Employee uğurla yeniləndi. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                employee.Id,
                employee.CompanyId);

            return new BaseResponse
            {
                Success = true,
                Message = "Employee updated successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UpdateEmployeeCommand zamanı xəta baş verdi. EmployeeId: {EmployeeId}",
                request.Id);

            throw;
        }
    }
}
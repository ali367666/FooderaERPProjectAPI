using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Employees.Commands.Create;

public class CreateEmployeeCommandHandler
    : IRequestHandler<CreateEmployeeCommand, BaseResponse<int>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        IRestaurantRepository restaurantRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _restaurantRepository = restaurantRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var isSuperAdmin = _currentUserService.IsSuperAdmin;
            var callerCompanyId = _currentUserService.CompanyId;

            _logger.LogInformation(
                "CreateEmployeeCommand başladı. CallerCompanyId: {CallerCompanyId}, IsSuperAdmin: {IsSuperAdmin}, FirstName: {FirstName}, LastName: {LastName}",
                callerCompanyId,
                isSuperAdmin,
                request.Request.FirstName,
                request.Request.LastName);

            // The Employee form has no separate "Company" field — a SuperAdmin targets a company by
            // picking one of its departments, so their employee is created under whichever company
            // that department belongs to. A tenant Admin is always confined to their own company.
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
                    "Employee yaradılmadı. Department tapılmadı. DepartmentId: {DepartmentId}, CallerCompanyId: {CallerCompanyId}",
                    request.Request.DepartmentId,
                    callerCompanyId);

                return new BaseResponse<int>
                {
                    Success = false,
                    Message = "Department not found."
                };
            }

            var position = await _positionRepository.GetByIdAsync(
                request.Request.PositionId,
                companyId,
                cancellationToken);

            if (position is null)
            {
                _logger.LogWarning(
                    "Employee yaradılmadı. Position tapılmadı. PositionId: {PositionId}, CompanyId: {CompanyId}",
                    request.Request.PositionId,
                    companyId);

                return new BaseResponse<int>
                {
                    Success = false,
                    Message = "Position not found."
                };
            }

            if (position.DepartmentId != request.Request.DepartmentId)
            {
                _logger.LogWarning(
                    "Employee yaradılmadı. Position department-ə aid deyil. PositionId: {PositionId}, DepartmentId: {DepartmentId}",
                    request.Request.PositionId,
                    request.Request.DepartmentId);

                return new BaseResponse<int>
                {
                    Success = false,
                    Message = "Position does not belong to selected department."
                };
            }

            var duplicateContact = await _employeeRepository.ExistsByEmailOrPhoneAsync(
                companyId,
                request.Request.Email,
                request.Request.PhoneNumber,
                excludeEmployeeId: null,
                cancellationToken);

            if (duplicateContact)
            {
                _logger.LogWarning(
                    "Employee yaradılmadı. Email/telefon artıq mövcuddur. CompanyId: {CompanyId}",
                    companyId);

                return new BaseResponse<int>
                {
                    Success = false,
                    Message = "An employee with this email or phone number already exists."
                };
            }

            var restaurant = await _restaurantRepository.GetByIdAsync(request.Request.RestaurantId, cancellationToken);

            if (restaurant is null || restaurant.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Employee yaradılmadı. Restaurant tapılmadı və ya şirkətə aid deyil. RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                    request.Request.RestaurantId,
                    companyId);

                return new BaseResponse<int>
                {
                    Success = false,
                    Message = "Branch not found."
                };
            }

            if (request.Request.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(
                    request.Request.UserId.Value,
                    cancellationToken);

                if (user is null || user.CompanyId != companyId)
                {
                    _logger.LogWarning(
                        "Employee yaradılmadı. User tapılmadı və ya başqa şirkətə aiddir. UserId: {UserId}, CompanyId: {CompanyId}",
                        request.Request.UserId.Value,
                        companyId);

                    return new BaseResponse<int>
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                var exists = await _employeeRepository.ExistsByUserIdAsync(
                    request.Request.UserId.Value,
                    cancellationToken);

                if (exists)
                {
                    _logger.LogWarning(
                        "Employee yaradılmadı. User artıq başqa employee-ə bağlıdır. UserId: {UserId}",
                        request.Request.UserId.Value);

                    return new BaseResponse<int>
                    {
                        Success = false,
                        Message = "User already assigned to another employee."
                    };
                }
            }

            var employee = new Employee
            {
                CompanyId = companyId,
                FirstName = request.Request.FirstName.Trim(),
                LastName = request.Request.LastName.Trim(),
                FatherName = request.Request.FatherName,

                PhoneNumber = request.Request.PhoneNumber,
                Email = request.Request.Email,
                Address = request.Request.Address,

                HireDate = request.Request.HireDate,

                DepartmentId = request.Request.DepartmentId,
                PositionId = request.Request.PositionId,
                RestaurantId = restaurant.Id,

                UserId = request.Request.UserId,
                IsActive = true
            };

            await _employeeRepository.AddAsync(employee, cancellationToken);
            await _employeeRepository.SaveChangesAsync(cancellationToken);

            try
            {
                await _auditLogService.LogAsync(
                    new AuditLogEntry
                    {
                        EntityName = "Employee",
                        EntityId = employee.Id.ToString(),
                        ActionType = "Create",
                        Message = $"Employee yaradıldı. Id: {employee.Id}, AdSoyad: {employee.FirstName} {employee.LastName}, DepartmentId: {employee.DepartmentId}, PositionId: {employee.PositionId}, CompanyId: {employee.CompanyId}",
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
                    "Employee create audit log yazılarkən xəta baş verdi. EmployeeId: {EmployeeId}",
                    employee.Id);
            }

            _logger.LogInformation(
                "Employee uğurla yaradıldı. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                employee.Id,
                employee.CompanyId);

            return new BaseResponse<int>
            {
                Success = true,
                Message = "Employee created successfully.",
                Data = employee.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CreateEmployeeCommand zamanı xəta baş verdi. FirstName: {FirstName}, LastName: {LastName}",
                request.Request.FirstName,
                request.Request.LastName);

            throw;
        }
    }
}
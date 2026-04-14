using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.User.Dtos.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.User.Commands.Create;

public class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IEmployeeRepository employeeRepository,
        IRestaurantRepository restaurantRepository,
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateUserCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _restaurantRepository = restaurantRepository;
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<CreateUserResponse> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.dto;
        var currentCompanyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "CreateUserCommand başladı. EmployeeId: {EmployeeId}, UserName: {UserName}, Email: {Email}, WorkplaceType: {WorkplaceType}, CompanyId: {CompanyId}",
            dto.EmployeeId,
            dto.UserName,
            dto.Email,
            dto.WorkplaceType,
            currentCompanyId);

        var employee = await _employeeRepository.GetByIdAsync(
            dto.EmployeeId,
            currentCompanyId,
            cancellationToken);

        if (employee is null)
        {
            _logger.LogWarning(
                "User yaradılmadı. Employee tapılmadı. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                dto.EmployeeId,
                currentCompanyId);

            throw new Exception("Employee tapılmadı.");
        }

        if (employee.UserId.HasValue)
        {
            _logger.LogWarning(
                "User yaradılmadı. Employee üçün artıq user mövcuddur. EmployeeId: {EmployeeId}, ExistingUserId: {ExistingUserId}",
                employee.Id,
                employee.UserId.Value);

            throw new Exception("Bu employee üçün artıq user yaradılıb.");
        }

        var trimmedEmail = dto.Email.Trim();
        var trimmedUserName = dto.UserName.Trim();

        var emailExists = await _identityService.EmailExistsAsync(trimmedEmail);
        if (emailExists)
        {
            _logger.LogWarning(
                "User yaradılmadı. Email artıq mövcuddur. Email: {Email}",
                trimmedEmail);

            throw new Exception("Bu email artıq mövcuddur.");
        }

        var userNameExists = await _identityService.UserNameExistsAsync(trimmedUserName);
        if (userNameExists)
        {
            _logger.LogWarning(
                "User yaradılmadı. Username artıq mövcuddur. UserName: {UserName}",
                trimmedUserName);

            throw new Exception("Bu username artıq mövcuddur.");
        }

        if (dto.WorkplaceType == EmployeeWorkplaceType.Restaurant)
        {
            if (!dto.RestaurantId.HasValue)
            {
                _logger.LogWarning(
                    "User yaradılmadı. WorkplaceType Restaurant olduqda RestaurantId göndərilməyib. EmployeeId: {EmployeeId}",
                    dto.EmployeeId);

                throw new Exception("Restaurant seçilməlidir.");
            }

            var restaurant = await _restaurantRepository.GetByIdAsync(
                dto.RestaurantId.Value,
                cancellationToken);

            if (restaurant is null || restaurant.CompanyId != currentCompanyId)
            {
                _logger.LogWarning(
                    "User yaradılmadı. Restaurant tapılmadı və ya şirkətə aid deyil. RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                    dto.RestaurantId.Value,
                    currentCompanyId);

                throw new Exception("Restaurant tapılmadı.");
            }
        }

        var restaurantId = dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice
            ? null
            : dto.RestaurantId;

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();

        var createResult = await _identityService.CreateUserAsync(
            fullName,
            trimmedUserName,
            trimmedEmail,
            dto.Password,
            dto.WorkplaceType,
            currentCompanyId,
            restaurantId
        );

        if (!createResult.Succeeded)
        {
            _logger.LogWarning(
                "User yaradılmadı. Identity create xətası. EmployeeId: {EmployeeId}, Errors: {Errors}",
                dto.EmployeeId,
                string.Join(", ", createResult.Errors));

            throw new Exception(string.Join(", ", createResult.Errors));
        }

        var roleAdded = await _identityService.AddToRoleAsync(createResult.UserId, "User");
        if (!roleAdded)
        {
            _logger.LogWarning(
                "User yaradıldı amma role əlavə olunmadı. UserId: {UserId}, EmployeeId: {EmployeeId}",
                createResult.UserId,
                dto.EmployeeId);

            throw new Exception("User role-a əlavə olunmadı.");
        }

        employee.UserId = createResult.UserId;
        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "User",
                    EntityId = createResult.UserId.ToString(),
                    ActionType = "Create",
                    Message = $"User yaradıldı. UserId: {createResult.UserId}, EmployeeId: {employee.Id}, FullName: {fullName}, UserName: {trimmedUserName}, Email: {trimmedEmail}, WorkplaceType: {dto.WorkplaceType}, CompanyId: {currentCompanyId}, RestaurantId: {restaurantId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "User üçün audit log yazıldı. UserId: {UserId}",
                createResult.UserId);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "User create audit log yazılarkən xəta baş verdi. UserId: {UserId}",
                createResult.UserId);
        }

        _logger.LogInformation(
            "User uğurla yaradıldı. UserId: {UserId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
            createResult.UserId,
            employee.Id,
            currentCompanyId);

        return new CreateUserResponse
        {
            Id = createResult.UserId,
            FullName = fullName,
            UserName = trimmedUserName,
            Email = trimmedEmail
        };
    }
}
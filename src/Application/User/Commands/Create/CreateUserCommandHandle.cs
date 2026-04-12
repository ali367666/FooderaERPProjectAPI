using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.User.Dtos.Responce;
using Domain.Enums;
using MediatR;

namespace Application.User.Commands.Create;

public class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public CreateUserCommandHandler(
        IEmployeeRepository employeeRepository,
        IRestaurantRepository restaurantRepository,
        IIdentityService identityService,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _restaurantRepository = restaurantRepository;
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<CreateUserResponse> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.dto;
        var currentCompanyId = _currentUserService.CompanyId;

        var employee = await _employeeRepository.GetByIdAsync(
            dto.EmployeeId,
            currentCompanyId,
            cancellationToken);

        if (employee is null)
            throw new Exception("Employee tapılmadı.");

        if (employee.UserId.HasValue)
            throw new Exception("Bu employee üçün artıq user yaradılıb.");

        var emailExists = await _identityService.EmailExistsAsync(dto.Email);
        if (emailExists)
            throw new Exception("Bu email artıq mövcuddur.");

        var userNameExists = await _identityService.UserNameExistsAsync(dto.UserName);
        if (userNameExists)
            throw new Exception("Bu username artıq mövcuddur.");

        if (dto.WorkplaceType == EmployeeWorkplaceType.Restaurant)
        {
            if (!dto.RestaurantId.HasValue)
                throw new Exception("Restaurant seçilməlidir.");

            var restaurant = await _restaurantRepository.GetByIdAsync(
                dto.RestaurantId.Value,
                cancellationToken);

            if (restaurant is null || restaurant.CompanyId != currentCompanyId)
                throw new Exception("Restaurant tapılmadı.");
        }

        var restaurantId = dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice
            ? null
            : dto.RestaurantId;

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();

        var createResult = await _identityService.CreateUserAsync(
            fullName,
            dto.UserName.Trim(),
            dto.Email.Trim(),
            dto.Password,
            dto.WorkplaceType,
            currentCompanyId,
            restaurantId
        );

        if (!createResult.Succeeded)
            throw new Exception(string.Join(", ", createResult.Errors));

        var roleAdded = await _identityService.AddToRoleAsync(createResult.UserId, "User");
        if (!roleAdded)
            throw new Exception("User role-a əlavə olunmadı.");

        employee.UserId = createResult.UserId;
        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse
        {
            Id = createResult.UserId,
            FullName = fullName,
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim()
        };
    }
}
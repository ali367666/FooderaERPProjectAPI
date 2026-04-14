using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Application.User.Dtos.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.User.Commands.Update;

public class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, BaseResponse<UpdateUserResponseDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IIdentityService _identityService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        ICompanyRepository companyRepository,
        IRestaurantRepository restaurantRepository,
        IIdentityService identityService,
        IAuditLogService auditLogService,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _companyRepository = companyRepository;
        _restaurantRepository = restaurantRepository;
        _identityService = identityService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<UpdateUserResponseDto>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.dto;

        _logger.LogInformation(
            "UpdateUserCommand başladı. UserId: {UserId}, FullName: {FullName}, WorkplaceType: {WorkplaceType}, CompanyId: {CompanyId}",
            request.id,
            dto.FullName,
            dto.WorkplaceType,
            dto.CompanyId);

        var user = await _identityService.GetUserByIdAsync(request.id);
        if (user is null)
        {
            _logger.LogWarning(
                "User yenilənmədi. User tapılmadı. UserId: {UserId}",
                request.id);

            return BaseResponse<UpdateUserResponseDto>.Fail("User tapılmadı.");
        }

        var company = await _companyRepository.GetByIdAsync(dto.CompanyId, cancellationToken);
        if (company is null)
        {
            _logger.LogWarning(
                "User yenilənmədi. Company tapılmadı. UserId: {UserId}, CompanyId: {CompanyId}",
                request.id,
                dto.CompanyId);

            return BaseResponse<UpdateUserResponseDto>.Fail("Company tapılmadı.");
        }

        string? restaurantName = null;
        int? restaurantId = null;

        if (dto.WorkplaceType == EmployeeWorkplaceType.Restaurant)
        {
            if (!dto.RestaurantId.HasValue)
            {
                _logger.LogWarning(
                    "User yenilənmədi. Restaurant workplace üçün RestaurantId göndərilməyib. UserId: {UserId}",
                    request.id);

                return BaseResponse<UpdateUserResponseDto>.Fail("RestaurantId göndərilməlidir.");
            }

            var restaurant = await _restaurantRepository.GetByIdAsync(dto.RestaurantId.Value, cancellationToken);
            if (restaurant is null)
            {
                _logger.LogWarning(
                    "User yenilənmədi. Restaurant tapılmadı. UserId: {UserId}, RestaurantId: {RestaurantId}",
                    request.id,
                    dto.RestaurantId.Value);

                return BaseResponse<UpdateUserResponseDto>.Fail("Restaurant tapılmadı.");
            }

            if (restaurant.CompanyId != dto.CompanyId)
            {
                _logger.LogWarning(
                    "User yenilənmədi. Restaurant company-ə aid deyil. UserId: {UserId}, RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                    request.id,
                    restaurant.Id,
                    dto.CompanyId);

                return BaseResponse<UpdateUserResponseDto>.Fail("Seçilən restaurant bu company-ə aid deyil.");
            }

            restaurantId = restaurant.Id;
            restaurantName = restaurant.Name;
        }

        var oldFullName = user.FullName;
        var oldWorkplaceType = user.WorkplaceType;
        var oldCompanyId = user.CompanyId;
        var oldRestaurantId = user.RestaurantId;

        var updateResult = await _identityService.UpdateUserAsync(
            request.id,
            dto.FullName,
            dto.WorkplaceType,
            dto.CompanyId,
            dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice ? null : restaurantId
        );

        if (!updateResult.Succeeded)
        {
            _logger.LogWarning(
                "User yenilənmədi. Identity update xətası. UserId: {UserId}, Errors: {Errors}",
                request.id,
                string.Join(", ", updateResult.Errors));

            return new BaseResponse<UpdateUserResponseDto>
            {
                Success = false,
                Message = "User yenilənmədi.",
                Errors = updateResult.Errors
            };
        }

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "User",
                    EntityId = request.id.ToString(),
                    ActionType = "Update",
                    Message = $"User yeniləndi. UserId: {request.id}, OldFullName: {oldFullName}, NewFullName: {dto.FullName}, OldWorkplaceType: {oldWorkplaceType}, NewWorkplaceType: {dto.WorkplaceType}, OldCompanyId: {oldCompanyId}, NewCompanyId: {dto.CompanyId}, OldRestaurantId: {oldRestaurantId}, NewRestaurantId: {(dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice ? null : restaurantId)}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "User üçün audit log yazıldı. UserId: {UserId}",
                request.id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "User update audit log yazılarkən xəta baş verdi. UserId: {UserId}",
                request.id);
        }

        var response = new UpdateUserResponseDto
        {
            Id = request.id,
            FullName = dto.FullName,
            WorkplaceType = dto.WorkplaceType,
            CompanyId = dto.CompanyId,
            CompanyName = company.Name,
            RestaurantId = dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice ? null : restaurantId,
            RestaurantName = restaurantName
        };

        _logger.LogInformation(
            "User uğurla yeniləndi. UserId: {UserId}, CompanyId: {CompanyId}",
            request.id,
            dto.CompanyId);

        return BaseResponse<UpdateUserResponseDto>.Ok(response, "User uğurla yeniləndi.");
    }
}
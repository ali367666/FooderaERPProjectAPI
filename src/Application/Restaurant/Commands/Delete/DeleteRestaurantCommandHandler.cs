using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Restaurant.Commands.Delete;

public sealed class DeleteRestaurantCommandHandler
    : IRequestHandler<DeleteRestaurantCommand, BaseResponse>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteRestaurantCommandHandler> _logger;

    public DeleteRestaurantCommandHandler(
        IRestaurantRepository restaurantRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<DeleteRestaurantCommandHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteRestaurantCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteRestaurantCommand başladı. RestaurantId: {RestaurantId}",
            request.Id);

        var restaurant = await _restaurantRepository.GetByIdAsync(request.Id, cancellationToken);

        if (restaurant is null)
        {
            _logger.LogWarning(
                "Restaurant silinmədi. Restaurant tapılmadı. RestaurantId: {RestaurantId}",
                request.Id);

            return BaseResponse.Fail("Restaurant tapılmadı.");
        }

        // 🔥 Köhnə məlumatları saxla (audit üçün)
        var oldName = restaurant.Name;
        var oldCompanyId = restaurant.CompanyId;

        _restaurantRepository.Delete(restaurant);
        await _restaurantRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Restaurant",
                    EntityId = restaurant.Id.ToString(),
                    ActionType = "Delete",
                    Message = $"Restaurant silindi. Id: {restaurant.Id}, Name: {oldName}, CompanyId: {oldCompanyId}",
                    IsSuccess = true,
                    UserId = _currentUserService.UserId
                },
                cancellationToken);

            _logger.LogInformation(
                "Restaurant üçün audit log yazıldı. RestaurantId: {RestaurantId}",
                restaurant.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Restaurant delete audit log yazılarkən xəta baş verdi. RestaurantId: {RestaurantId}",
                restaurant.Id);
        }

        _logger.LogInformation(
            "Restaurant uğurla silindi. RestaurantId: {RestaurantId}, Name: {Name}",
            restaurant.Id,
            oldName);

        return BaseResponse.Ok("Restaurant uğurla silindi.");
    }
}
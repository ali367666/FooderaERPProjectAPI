using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.RestaurantTables.Commands.Delete;

public class DeleteRestaurantTableCommandHandler : IRequestHandler<DeleteRestaurantTableCommand>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteRestaurantTableCommandHandler> _logger;

    public DeleteRestaurantTableCommandHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<DeleteRestaurantTableCommandHandler> logger)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task Handle(DeleteRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "DeleteRestaurantTableCommand başladı. TableId: {TableId}, CompanyId: {CompanyId}",
            request.Id,
            companyId);

        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (table is null)
        {
            _logger.LogWarning(
                "RestaurantTable silinmədi. Masa tapılmadı. TableId: {TableId}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            throw new Exception("Masa tapılmadı.");
        }

        if (table.IsOccupied)
        {
            _logger.LogWarning(
                "RestaurantTable silinmədi. Masa doludur. TableId: {TableId}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            throw new Exception("Dolu masa silinə bilməz.");
        }

        var oldRestaurantId = table.RestaurantId;
        var oldName = table.Name;
        var oldCapacity = table.Capacity;
        var oldIsActive = table.IsActive;
        var oldIsOccupied = table.IsOccupied;

        _restaurantTableRepository.Delete(table);
        await _restaurantTableRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "RestaurantTable",
                    EntityId = request.Id.ToString(),
                    ActionType = "Delete",
                    Message = $"RestaurantTable silindi. Id: {request.Id}, RestaurantId: {oldRestaurantId}, Name: {oldName}, Capacity: {oldCapacity}, IsActive: {oldIsActive}, IsOccupied: {oldIsOccupied}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "RestaurantTable üçün audit log yazıldı. TableId: {TableId}",
                request.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "RestaurantTable delete audit log yazılarkən xəta baş verdi. TableId: {TableId}",
                request.Id);
        }

        _logger.LogInformation(
            "RestaurantTable uğurla silindi. TableId: {TableId}, CompanyId: {CompanyId}",
            request.Id,
            companyId);
    }
}
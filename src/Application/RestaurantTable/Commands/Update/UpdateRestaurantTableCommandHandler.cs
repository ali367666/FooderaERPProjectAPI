using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.RestaurantTables.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.RestaurantTables.Commands.Update;

public class UpdateRestaurantTableCommandHandler
    : IRequestHandler<UpdateRestaurantTableCommand, RestaurantTableResponse>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateRestaurantTableCommandHandler> _logger;

    public UpdateRestaurantTableCommandHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<UpdateRestaurantTableCommandHandler> logger)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<RestaurantTableResponse> Handle(
        UpdateRestaurantTableCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;
        var trimmedName = dto.Name.Trim();

        _logger.LogInformation(
            "UpdateRestaurantTableCommand başladı. TableId: {TableId}, Name: {Name}, RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
            request.Id,
            trimmedName,
            dto.RestaurantId,
            companyId);

        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (table is null)
        {
            _logger.LogWarning(
                "RestaurantTable update olunmadı. Masa tapılmadı. TableId: {TableId}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            throw new Exception("Masa tapılmadı.");
        }

        var exists = await _restaurantTableRepository.ExistsByNameAsync(
            companyId,
            dto.RestaurantId,
            table.Id,
            trimmedName,
            cancellationToken);

        var restaurantExists = await _restaurantTableRepository.RestaurantExistsAsync(
            companyId,
            dto.RestaurantId,
            cancellationToken);

        if (!restaurantExists)
        {
            _logger.LogWarning(
                "RestaurantTable update olunmadı. Restaurant tapılmadı və ya tenant-a aid deyil. RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                dto.RestaurantId,
                companyId);

            throw new Exception("Restaurant tapılmadı.");
        }

        if (exists)
        {
            _logger.LogWarning(
                "RestaurantTable update olunmadı. Eyni adda masa artıq mövcuddur. TableId: {TableId}, Name: {Name}, RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                request.Id,
                trimmedName,
                dto.RestaurantId,
                companyId);

            throw new Exception("Bu restoranda bu adda başqa masa artıq mövcuddur.");
        }

        // 🔥 OLD VALUES (audit üçün)
        var oldRestaurantId = table.RestaurantId;
        var oldName = table.Name;
        var oldCapacity = table.Capacity;
        var oldIsActive = table.IsActive;

        // UPDATE
        table.RestaurantId = dto.RestaurantId;
        table.Name = trimmedName;
        table.Capacity = dto.Capacity;
        table.IsActive = dto.IsActive;

        _restaurantTableRepository.Update(table);
        await _restaurantTableRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "RestaurantTable",
                    EntityId = table.Id.ToString(),
                    ActionType = "Update",
                    Message = $"RestaurantTable yeniləndi. Id: {table.Id}, " +
                              $"OldName: {oldName}, NewName: {table.Name}, " +
                              $"OldRestaurantId: {oldRestaurantId}, NewRestaurantId: {table.RestaurantId}, " +
                              $"OldCapacity: {oldCapacity}, NewCapacity: {table.Capacity}, " +
                              $"OldIsActive: {oldIsActive}, NewIsActive: {table.IsActive}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "RestaurantTable üçün audit log yazıldı. TableId: {TableId}",
                table.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "RestaurantTable update audit log yazılarkən xəta baş verdi. TableId: {TableId}",
                table.Id);
        }

        _logger.LogInformation(
            "RestaurantTable uğurla yeniləndi. TableId: {TableId}, RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
            table.Id,
            table.RestaurantId,
            companyId);

        return new RestaurantTableResponse
        {
            Id = table.Id,
            RestaurantId = table.RestaurantId,
            RestaurantName = (await _restaurantTableRepository.GetByIdAsync(table.Id, companyId, cancellationToken))?.Restaurant?.Name ?? string.Empty,
            Name = table.Name,
            Capacity = table.Capacity,
            IsActive = table.IsActive,
            IsOccupied = table.IsOccupied
        };
    }
}
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.RestaurantTables.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.RestaurantTables.Commands.Create;

public class CreateRestaurantTableCommandHandler
    : IRequestHandler<CreateRestaurantTableCommand, RestaurantTableResponse>
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateRestaurantTableCommandHandler> _logger;

    public CreateRestaurantTableCommandHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateRestaurantTableCommandHandler> logger)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<RestaurantTableResponse> Handle(
        CreateRestaurantTableCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;
        var companyId = _currentUserService.CompanyId;
        var trimmedName = dto.Name.Trim();

        _logger.LogInformation(
            "CreateRestaurantTableCommand başladı. RestaurantId: {RestaurantId}, Name: {Name}, CompanyId: {CompanyId}",
            dto.RestaurantId,
            trimmedName,
            companyId);

        var exists = await _restaurantTableRepository.ExistsByNameAsync(
            companyId,
            dto.RestaurantId,
            trimmedName,
            cancellationToken);

        var restaurantExists = await _restaurantTableRepository.RestaurantExistsAsync(
            companyId,
            dto.RestaurantId,
            cancellationToken);

        if (!restaurantExists)
        {
            _logger.LogWarning(
                "RestaurantTable yaradılmadı. Restaurant tapılmadı və ya tenant-a aid deyil. RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                dto.RestaurantId,
                companyId);

            throw new Exception("Restaurant tapılmadı.");
        }

        if (exists)
        {
            _logger.LogWarning(
                "RestaurantTable yaradılmadı. Eyni adda masa artıq mövcuddur. RestaurantId: {RestaurantId}, Name: {Name}, CompanyId: {CompanyId}",
                dto.RestaurantId,
                trimmedName,
                companyId);

            throw new Exception("Bu restoranda bu adda masa artıq mövcuddur.");
        }

        var table = new Domain.Entities.RestaurantTable
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            Name = trimmedName,
            Capacity = dto.Capacity,
            IsActive = true,
            IsOccupied = false
        };

        await _restaurantTableRepository.AddAsync(table, cancellationToken);
        await _restaurantTableRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "RestaurantTable",
                    EntityId = table.Id.ToString(),
                    ActionType = "Create",
                    Message = $"RestaurantTable yaradıldı. Id: {table.Id}, RestaurantId: {table.RestaurantId}, Name: {table.Name}, Capacity: {table.Capacity}, IsActive: {table.IsActive}, IsOccupied: {table.IsOccupied}",
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
                "RestaurantTable create audit log yazılarkən xəta baş verdi. TableId: {TableId}",
                table.Id);
        }

        _logger.LogInformation(
            "RestaurantTable uğurla yaradıldı. TableId: {TableId}, RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
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
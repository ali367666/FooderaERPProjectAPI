using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.MenuCategories.Commands.Update;

public class UpdateMenuCategoryCommandHandler
    : IRequestHandler<UpdateMenuCategoryCommand>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateMenuCategoryCommandHandler> _logger;

    public UpdateMenuCategoryCommandHandler(
        IMenuCategoryRepository menuCategoryRepository,
        IAuditLogService auditLogService,
        ILogger<UpdateMenuCategoryCommandHandler> logger)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task Handle(UpdateMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateMenuCategoryCommand başladı. Id: {Id}, CompanyId: {CompanyId}",
            request.Id,
            request.CompanyId);

        var entity = await _menuCategoryRepository.GetByIdAsync(
            request.Id,
            request.CompanyId,
            cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning(
                "MenuCategory update olunmadı. Tapılmadı. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                request.CompanyId);

            throw new NotFoundException("Menu kateqoriyası tapılmadı.");
        }

        var normalizedName = request.Request.Name.Trim();

        var duplicateExists = await _menuCategoryRepository.ExistsByNameAsync(
            request.CompanyId,
            normalizedName,
            cancellationToken);

        if (duplicateExists &&
            !string.Equals(entity.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "MenuCategory update olunmadı. Duplicate name. Id: {Id}, Name: {Name}",
                request.Id,
                normalizedName);

            throw new BadRequestException("Bu adda menu kateqoriyası artıq mövcuddur.");
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            entity.Id,
            entity.CompanyId,
            entity.Name,
            entity.Description,
            entity.IsActive
        });

        entity.Name = normalizedName;
        entity.Description = string.IsNullOrWhiteSpace(request.Request.Description)
            ? null
            : request.Request.Description.Trim();
        entity.IsActive = request.Request.IsActive;

        _menuCategoryRepository.Update(entity);
        await _menuCategoryRepository.SaveChangesAsync(cancellationToken);

        var newValues = JsonSerializer.Serialize(new
        {
            entity.Id,
            entity.CompanyId,
            entity.Name,
            entity.Description,
            entity.IsActive
        });

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "MenuCategory",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Update",
                    OldValues = oldValues,
                    NewValues = newValues,
                    Message = $"MenuCategory yeniləndi. Id: {entity.Id}, Ad: {entity.Name}, CompanyId: {entity.CompanyId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "MenuCategory üçün audit log yazıldı. MenuCategoryId: {MenuCategoryId}",
                entity.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "MenuCategory update audit log yazılarkən xəta baş verdi. MenuCategoryId: {MenuCategoryId}",
                entity.Id);
        }

        _logger.LogInformation(
            "MenuCategory uğurla yeniləndi. Id: {Id}, CompanyId: {CompanyId}",
            entity.Id,
            entity.CompanyId);
    }
}
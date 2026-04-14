using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.MenuCategories.Commands.Delete;

public class DeleteMenuCategoryCommandHandler
    : IRequestHandler<DeleteMenuCategoryCommand>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteMenuCategoryCommandHandler> _logger;

    public DeleteMenuCategoryCommandHandler(
        IMenuCategoryRepository menuCategoryRepository,
        IAuditLogService auditLogService,
        ILogger<DeleteMenuCategoryCommandHandler> logger)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task Handle(DeleteMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteMenuCategoryCommand başladı. Id: {Id}, CompanyId: {CompanyId}",
            request.Id,
            request.CompanyId);

        var entity = await _menuCategoryRepository.GetByIdAsync(
            request.Id,
            request.CompanyId,
            cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning(
                "MenuCategory silinmədi. Tapılmadı. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                request.CompanyId);

            throw new NotFoundException("Menu kateqoriyası tapılmadı.");
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            entity.Id,
            entity.CompanyId,
            entity.Name,
            entity.Description,
            entity.IsActive
        });

        _menuCategoryRepository.Delete(entity);
        await _menuCategoryRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "MenuCategory",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Delete",
                    OldValues = oldValues,
                    NewValues = null,
                    Message = $"MenuCategory silindi. Id: {entity.Id}, Ad: {entity.Name}, CompanyId: {entity.CompanyId}",
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
                "MenuCategory delete audit log yazılarkən xəta baş verdi. MenuCategoryId: {MenuCategoryId}",
                entity.Id);
        }

        _logger.LogInformation(
            "MenuCategory uğurla silindi. MenuCategoryId: {MenuCategoryId}, CompanyId: {CompanyId}",
            entity.Id,
            entity.CompanyId);
    }
}
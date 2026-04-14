using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.MenuItems.Commands.Delete;

public class DeleteMenuItemCommandHandler
    : IRequestHandler<DeleteMenuItemCommand>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteMenuItemCommandHandler> _logger;

    public DeleteMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<DeleteMenuItemCommandHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task Handle(DeleteMenuItemCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "DeleteMenuItemCommand başladı. Id: {Id}, CompanyId: {CompanyId}",
            request.Id,
            companyId);

        if (companyId == 0)
        {
            _logger.LogWarning("MenuItem silinmədi. CompanyId tapılmadı.");
            throw new BadRequestException("CompanyId tapılmadı.");
        }

        var entity = await _menuItemRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning(
                "MenuItem silinmədi. Tapılmadı. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            throw new NotFoundException("Menu məhsulu tapılmadı.");
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            entity.Id,
            entity.CompanyId,
            entity.Name,
            entity.Description,
            entity.Price,
            entity.Portion,
            entity.MenuCategoryId,
            entity.PreparationType,
            entity.IsActive
        });

        _menuItemRepository.Delete(entity);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "MenuItem",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Delete",
                    OldValues = oldValues,
                    NewValues = null,
                    Message = $"MenuItem silindi. Id: {entity.Id}, Ad: {entity.Name}, MenuCategoryId: {entity.MenuCategoryId}, PreparationType: {entity.PreparationType}, CompanyId: {entity.CompanyId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "MenuItem üçün audit log yazıldı. MenuItemId: {MenuItemId}",
                entity.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "MenuItem delete audit log yazılarkən xəta baş verdi. MenuItemId: {MenuItemId}",
                entity.Id);
        }

        _logger.LogInformation(
            "MenuItem uğurla silindi. MenuItemId: {MenuItemId}, CompanyId: {CompanyId}",
            entity.Id,
            entity.CompanyId);
    }
}
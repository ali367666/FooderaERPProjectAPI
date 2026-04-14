using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.MenuItems.Commands.Create;

public class CreateMenuItemCommandHandler
    : IRequestHandler<CreateMenuItemCommand, int>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateMenuItemCommandHandler> _logger;

    public CreateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateMenuItemCommandHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<int> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "CreateMenuItemCommand başladı. CompanyId: {CompanyId}, Name: {Name}, MenuCategoryId: {MenuCategoryId}",
            companyId,
            request.Request.Name,
            request.Request.MenuCategoryId);

        if (companyId == 0)
        {
            _logger.LogWarning("MenuItem yaradılmadı. CompanyId tapılmadı.");
            throw new BadRequestException("CompanyId tapılmadı.");
        }

        var normalizedName = request.Request.Name.Trim();

        var categoryExists = await _menuCategoryRepository.ExistsByIdAsync(
            request.Request.MenuCategoryId,
            companyId,
            cancellationToken);

        if (!categoryExists)
        {
            _logger.LogWarning(
                "MenuItem yaradılmadı. MenuCategory tapılmadı. MenuCategoryId: {MenuCategoryId}, CompanyId: {CompanyId}",
                request.Request.MenuCategoryId,
                companyId);

            throw new NotFoundException("Menu kateqoriyası tapılmadı.");
        }

        var exists = await _menuItemRepository.ExistsByNameAsync(
            companyId,
            request.Request.MenuCategoryId,
            normalizedName,
            cancellationToken);

        if (exists)
        {
            _logger.LogWarning(
                "MenuItem yaradılmadı. Duplicate name. CompanyId: {CompanyId}, MenuCategoryId: {MenuCategoryId}, Name: {Name}",
                companyId,
                request.Request.MenuCategoryId,
                normalizedName);

            throw new BadRequestException("Bu adda menu məhsulu artıq mövcuddur.");
        }

        var entity = new MenuItem
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(request.Request.Description)
                ? null
                : request.Request.Description.Trim(),
            Price = request.Request.Price,
            Portion = string.IsNullOrWhiteSpace(request.Request.Portion)
                ? null
                : request.Request.Portion.Trim(),
            MenuCategoryId = request.Request.MenuCategoryId,
            PreparationType = request.Request.PreparationType,
            IsActive = true,
            CompanyId = companyId
        };

        await _menuItemRepository.AddAsync(entity, cancellationToken);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "MenuItem",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Create",
                    Message = $"MenuItem yaradıldı. Id: {entity.Id}, Ad: {entity.Name}, MenuCategoryId: {entity.MenuCategoryId}, PreparationType: {entity.PreparationType}, CompanyId: {entity.CompanyId}",
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
                "MenuItem create audit log yazılarkən xəta baş verdi. MenuItemId: {MenuItemId}",
                entity.Id);
        }

        _logger.LogInformation(
            "MenuItem uğurla yaradıldı. MenuItemId: {MenuItemId}, CompanyId: {CompanyId}",
            entity.Id,
            entity.CompanyId);

        return entity.Id;
    }
}
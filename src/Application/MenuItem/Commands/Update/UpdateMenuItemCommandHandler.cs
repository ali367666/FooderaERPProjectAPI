using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.MenuItems.Commands.Update;

public class UpdateMenuItemCommandHandler
    : IRequestHandler<UpdateMenuItemCommand>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IMenuItemTypeRepository _menuItemTypeRepository;
    private readonly IMenuItemSetComponentRepository _setComponentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateMenuItemCommandHandler> _logger;

    public UpdateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        IMenuItemTypeRepository menuItemTypeRepository,
        IMenuItemSetComponentRepository setComponentRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<UpdateMenuItemCommandHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _menuItemTypeRepository = menuItemTypeRepository;
        _setComponentRepository = setComponentRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "UpdateMenuItemCommand başladı. Id: {Id}, CompanyId: {CompanyId}",
            request.Id,
            companyId);

        if (companyId == 0)
        {
            _logger.LogWarning("MenuItem update olunmadı. CompanyId tapılmadı.");
            throw new BadRequestException("CompanyId tapılmadı.");
        }

        var entity = await _menuItemRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning(
                "MenuItem update olunmadı. Tapılmadı. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            throw new NotFoundException("Menu məhsulu tapılmadı.");
        }

        var normalizedName = request.Request.Name.Trim();

        var categoryExists = await _menuCategoryRepository.ExistsByIdAsync(
            request.Request.MenuCategoryId,
            companyId,
            cancellationToken);

        if (!categoryExists)
        {
            _logger.LogWarning(
                "MenuItem update olunmadı. MenuCategory tapılmadı. MenuCategoryId: {MenuCategoryId}, CompanyId: {CompanyId}",
                request.Request.MenuCategoryId,
                companyId);

            throw new NotFoundException("Menu kateqoriyası tapılmadı.");
        }

        var duplicateExists = await _menuItemRepository.ExistsByNameAsync(
            companyId,
            request.Request.MenuCategoryId,
            normalizedName,
            cancellationToken);

        if (duplicateExists &&
            !(string.Equals(entity.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
              && entity.MenuCategoryId == request.Request.MenuCategoryId))
        {
            _logger.LogWarning(
                "MenuItem update olunmadı. Duplicate name. Id: {Id}, Name: {Name}, MenuCategoryId: {MenuCategoryId}",
                request.Id,
                normalizedName,
                request.Request.MenuCategoryId);

            throw new BadRequestException("Bu adda menu məhsulu artıq mövcuddur.");
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

        entity.Name = normalizedName;
        entity.Description = string.IsNullOrWhiteSpace(request.Request.Description)
            ? null
            : request.Request.Description.Trim();
        entity.ImageUrl = string.IsNullOrWhiteSpace(request.Request.ImageUrl)
            ? null
            : request.Request.ImageUrl.Trim();
        entity.Portion = string.IsNullOrWhiteSpace(request.Request.Portion)
            ? null
            : request.Request.Portion.Trim();
        entity.MenuCategoryId = request.Request.MenuCategoryId;
        entity.PreparationType = request.Request.PreparationType;
        entity.IsActive = request.Request.IsActive;

        var itemType = await _menuItemTypeRepository.GetByIdAsync(
            request.Request.ItemTypeId,
            companyId,
            cancellationToken);

        if (itemType is null)
        {
            _logger.LogWarning(
                "MenuItem update olunmadı. ItemType tapılmadı. ItemTypeId: {ItemTypeId}, CompanyId: {CompanyId}",
                request.Request.ItemTypeId,
                companyId);

            throw new NotFoundException("Məhsul növü tapılmadı.");
        }

        entity.ItemTypeId = itemType.Id;
        entity.UnitId = request.Request.UnitId;
        entity.VatPercent = request.Request.VatPercent;
        entity.Barcode = string.IsNullOrWhiteSpace(request.Request.Barcode) ? null : request.Request.Barcode.Trim();
        if (request.Request.ResetWeightCode)
            entity.WeightCode = $"{companyId}-{entity.Id:D6}-{DateTime.UtcNow:HHmmss}";

        if (_currentUserService.HasPermission(Domain.Constants.AppPermissions.PosOverridePrice))
        {
            entity.Price = request.Request.Price;
            entity.StationPrice = request.Request.StationPrice;
            entity.PurchasePrice = request.Request.PurchasePrice;
            entity.PackagePrice = request.Request.PackagePrice;
            entity.SpecialPrice1 = request.Request.SpecialPrice1;
            entity.SpecialPrice2 = request.Request.SpecialPrice2;
            entity.SpecialPrice3 = request.Request.SpecialPrice3;
            entity.SpecialPrice4 = request.Request.SpecialPrice4;
            entity.SpecialPrice5 = request.Request.SpecialPrice5;
        }

        entity.HideFromPosSearch = request.Request.HideFromPosSearch;
        entity.HideBarcode = request.Request.HideBarcode;
        entity.ExcludeFromDiscount = request.Request.ExcludeFromDiscount;
        entity.SkipTaxCalculation = request.Request.SkipTaxCalculation;
        entity.IsTimeBased = request.Request.IsTimeBased;
        entity.AllowQuantityPromptOverride = request.Request.AllowQuantityPromptOverride;
        entity.PrinterId = request.Request.PrinterId;
        entity.IsSet = request.Request.IsSet;
        entity.StockItemId = request.Request.StockItemId;

        _menuItemRepository.Update(entity);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);

        // SET tərkibini tam yenidən yaz (sadə diff: köhnələri sil, yenilərini əlavə et)
        var existingComponents = await _setComponentRepository.GetBySetMenuItemIdAsync(entity.Id, cancellationToken);
        if (existingComponents.Count > 0)
            _setComponentRepository.RemoveRange(existingComponents);

        if (entity.IsSet && request.Request.SetComponents.Count > 0)
        {
            foreach (var c in request.Request.SetComponents)
            {
                await _setComponentRepository.AddAsync(new Domain.Entities.MenuItemSetComponent
                {
                    SetMenuItemId = entity.Id,
                    ComponentMenuItemId = c.ComponentMenuItemId,
                    Quantity = c.Quantity < 1 ? 1 : c.Quantity
                }, cancellationToken);
            }
        }

        await _setComponentRepository.SaveChangesAsync(cancellationToken);

        var newValues = JsonSerializer.Serialize(new
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

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "MenuItem",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Update",
                    OldValues = oldValues,
                    NewValues = newValues,
                    Message = $"MenuItem yeniləndi. Id: {entity.Id}, Ad: {entity.Name}, MenuCategoryId: {entity.MenuCategoryId}, PreparationType: {entity.PreparationType}, CompanyId: {entity.CompanyId}",
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
                "MenuItem update audit log yazılarkən xəta baş verdi. MenuItemId: {MenuItemId}",
                entity.Id);
        }

        _logger.LogInformation(
            "MenuItem uğurla yeniləndi. MenuItemId: {MenuItemId}, CompanyId: {CompanyId}",
            entity.Id,
            entity.CompanyId);
    }
}
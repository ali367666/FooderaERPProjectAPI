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
    private readonly IMenuItemTypeRepository _menuItemTypeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateMenuItemCommandHandler> _logger;

    public CreateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        IMenuItemTypeRepository menuItemTypeRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateMenuItemCommandHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _menuItemTypeRepository = menuItemTypeRepository;
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

        var itemType = await _menuItemTypeRepository.GetByIdAsync(
            request.Request.ItemTypeId,
            companyId,
            cancellationToken);

        if (itemType is null)
        {
            _logger.LogWarning(
                "MenuItem yaradılmadı. ItemType tapılmadı. ItemTypeId: {ItemTypeId}, CompanyId: {CompanyId}",
                request.Request.ItemTypeId,
                companyId);

            throw new NotFoundException("Məhsul növü tapılmadı.");
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
            CompanyId = companyId,
            ItemTypeId = itemType.Id,
            UnitId = request.Request.UnitId,
            VatPercent = request.Request.VatPercent,
            Barcode = string.IsNullOrWhiteSpace(request.Request.Barcode) ? null : request.Request.Barcode.Trim(),
            StationPrice = request.Request.StationPrice,
            PurchasePrice = request.Request.PurchasePrice,
            PackagePrice = request.Request.PackagePrice,
            SpecialPrice1 = request.Request.SpecialPrice1,
            SpecialPrice2 = request.Request.SpecialPrice2,
            SpecialPrice3 = request.Request.SpecialPrice3,
            SpecialPrice4 = request.Request.SpecialPrice4,
            SpecialPrice5 = request.Request.SpecialPrice5,
            HideFromPosSearch = request.Request.HideFromPosSearch,
            HideBarcode = request.Request.HideBarcode,
            ExcludeFromDiscount = request.Request.ExcludeFromDiscount,
            SkipTaxCalculation = request.Request.SkipTaxCalculation,
            IsTimeBased = request.Request.IsTimeBased,
            AllowQuantityPromptOverride = request.Request.AllowQuantityPromptOverride,
            PrinterId = request.Request.PrinterId,
            IsSet = request.Request.IsSet,
            StockItemId = request.Request.StockItemId
        };

        await _menuItemRepository.AddAsync(entity, cancellationToken);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);

        entity.WeightCode = $"{companyId}-{entity.Id:D6}";
        _menuItemRepository.Update(entity);
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
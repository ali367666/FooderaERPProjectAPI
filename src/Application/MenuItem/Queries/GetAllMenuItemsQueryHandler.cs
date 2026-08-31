using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Queries.GetAll;

public class GetAllMenuItemsQueryHandler
    : IRequestHandler<GetAllMenuItemsQuery, List<MenuItemResponse>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllMenuItemsQueryHandler(
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<MenuItemResponse>> Handle(GetAllMenuItemsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        if (companyId == 0)
            throw new BadRequestException("CompanyId tapılmadı.");

        var entities = await _menuItemRepository.GetAllAsync(companyId, cancellationToken);

        return entities.Select(Map).ToList();
    }

    internal static MenuItemResponse Map(Domain.Entities.MenuItem entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Price = entity.Price,
        Portion = entity.Portion,
        IsActive = entity.IsActive,
        MenuCategoryId = entity.MenuCategoryId,
        MenuCategoryName = entity.MenuCategory.Name,
        PreparationType = entity.PreparationType,
        ItemTypeId = entity.ItemTypeId,
        ItemTypeName = entity.ItemType?.Name ?? "",
        UnitId = entity.UnitId,
        VatPercent = entity.VatPercent,
        WeightCode = entity.WeightCode,
        Barcode = entity.Barcode,
        StationPrice = entity.StationPrice,
        PurchasePrice = entity.PurchasePrice,
        PackagePrice = entity.PackagePrice,
        SpecialPrice1 = entity.SpecialPrice1,
        SpecialPrice2 = entity.SpecialPrice2,
        SpecialPrice3 = entity.SpecialPrice3,
        SpecialPrice4 = entity.SpecialPrice4,
        SpecialPrice5 = entity.SpecialPrice5,
        HideFromPosSearch = entity.HideFromPosSearch,
        HideBarcode = entity.HideBarcode,
        ExcludeFromDiscount = entity.ExcludeFromDiscount,
        SkipTaxCalculation = entity.SkipTaxCalculation,
        IsTimeBased = entity.IsTimeBased,
        AllowQuantityPromptOverride = entity.AllowQuantityPromptOverride,
        PrinterId = entity.PrinterId,
        IsSet = entity.IsSet,
        StockItemId = entity.StockItemId,
        StockItemName = entity.StockItem?.Name
    };
}
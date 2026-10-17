using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Queries.Search;

public class SearchMenuItemsQueryHandler : IRequestHandler<SearchMenuItemsQuery, List<MenuItemSearchResponse>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICompanySettingsRepository _settingsRepository;
    private readonly ICurrentUserService _currentUserService;

    public SearchMenuItemsQueryHandler(
        IMenuItemRepository menuItemRepository,
        ICompanySettingsRepository settingsRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _settingsRepository = settingsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<MenuItemSearchResponse>> Handle(SearchMenuItemsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        if (companyId == 0)
            throw new BadRequestException("CompanyId tapılmadı.");

        var items = await _menuItemRepository.GetAllAsync(companyId, cancellationToken);
        var settings = await _settingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        // Every typed word must appear somewhere in the name (start, middle or end), in any order.
        var words = SearchText.Fold(request.Term)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var code = request.Term?.Trim();

        return items
            .Where(i => i.IsActive && !i.HideFromPosSearch)
            .Select(i => new { Item = i, Key = SearchText.Fold(i.Name) })
            .Where(x => words.All(w => x.Key.Contains(w, StringComparison.Ordinal)) || MatchesCode(x.Item, code))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ThenBy(x => x.Item.Name, StringComparer.Ordinal)
            .Select(x => new MenuItemSearchResponse
            {
                Id = x.Item.Id,
                Name = x.Item.Name,
                Price = OrderLinePricing.ResolveUnitPrice(x.Item, request.IsDelivery, settings),
                UnitId = x.Item.UnitId,
                IsSet = x.Item.IsSet,
                IsTimeBased = x.Item.IsTimeBased,
                PreparationType = x.Item.PreparationType,
                MenuCategoryId = x.Item.MenuCategoryId,
                MenuCategoryName = x.Item.MenuCategory.Name
            })
            .ToList();
    }

    /// <summary>A scanned barcode / weight code finds the product exactly, unless its code is hidden.</summary>
    private static bool MatchesCode(Domain.Entities.MenuItem item, string? code) =>
        !string.IsNullOrEmpty(code) && !item.HideBarcode
        && (item.Barcode == code || item.WeightCode == code);
}

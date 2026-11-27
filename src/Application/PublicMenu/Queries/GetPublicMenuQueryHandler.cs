using System.Text.Json;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PublicMenu.Dtos;
using Application.PublicMenu.Services;
using MediatR;

namespace Application.PublicMenu.Queries;

public class GetPublicMenuQueryHandler : IRequestHandler<GetPublicMenuQuery, PublicMenuResponse?>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly IQrMenuRepository _qrMenuRepository;
    private readonly IPublicMenuBuilder _menuBuilder;

    public GetPublicMenuQueryHandler(
        IRestaurantRepository restaurantRepository,
        ICompanySettingsRepository companySettingsRepository,
        IQrMenuRepository qrMenuRepository,
        IPublicMenuBuilder menuBuilder)
    {
        _restaurantRepository = restaurantRepository;
        _companySettingsRepository = companySettingsRepository;
        _qrMenuRepository = qrMenuRepository;
        _menuBuilder = menuBuilder;
    }

    public async Task<PublicMenuResponse?> Handle(GetPublicMenuQuery request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null)
            return null;

        var companyId = restaurant.CompanyId;
        var settings = await _companySettingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        // The published menu when there is one (products added or re-priced since stay hidden until the
        // next publish); a branch that never published still shows its live menu.
        var snapshot = await _qrMenuRepository.GetSnapshotAsync(request.RestaurantId, cancellationToken);
        var categories = snapshot is null
            ? await _menuBuilder.BuildCategoriesAsync(companyId, cancellationToken)
            : JsonSerializer.Deserialize<List<PublicMenuCategoryResponse>>(snapshot.MenuJson) ?? new();

        // Stock changes by the minute, so availability is always read live.
        var outOfStockIds = await _menuBuilder.GetOutOfStockMenuItemIdsAsync(companyId, request.RestaurantId, cancellationToken);
        foreach (var item in categories.SelectMany(c => c.Items))
            item.IsAvailable = !outOfStockIds.Contains(item.Id);

        // A table's own QR carries its token; an unknown token just shows the plain menu.
        var table = string.IsNullOrWhiteSpace(request.TableToken)
            ? null
            : await _qrMenuRepository.FindTableByTokenAsync(request.RestaurantId, request.TableToken.Trim(), cancellationToken);

        return new PublicMenuResponse
        {
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            LogoUrl = settings?.LoginLogoUrl,
            Slogan = settings?.Slogan,
            ProductColor = settings?.ProductColor,
            ContactPhoneNumber = settings?.ContactPhoneNumber,
            SocialLinks = settings?.SocialLinks,
            TableName = table?.Name,
            TableToken = table?.QrToken,
            Categories = categories
        };
    }
}

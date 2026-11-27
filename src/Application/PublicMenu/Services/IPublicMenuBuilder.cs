using Application.PublicMenu.Dtos;

namespace Application.PublicMenu.Services;

public interface IPublicMenuBuilder
{
    /// <summary>The company's current categories and products as a customer sees them, all marked available.</summary>
    Task<List<PublicMenuCategoryResponse>> BuildCategoriesAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Ids of the products the branch can't make right now (a recipe ingredient is out of stock).</summary>
    Task<HashSet<int>> GetOutOfStockMenuItemIdsAsync(int companyId, int restaurantId, CancellationToken cancellationToken);

    /// <summary>Fingerprint of the menu's content (names, prices, pictures...), independent of stock.</summary>
    string ComputeHash(IReadOnlyCollection<PublicMenuCategoryResponse> categories);
}

using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Queries.Search;

/// <summary>POS product search. An empty <see cref="Term"/> returns the whole menu, A–Z.</summary>
public class SearchMenuItemsQuery : IRequest<List<MenuItemSearchResponse>>
{
    public string? Term { get; set; }
    /// <summary>Own-courier delivery order: prices come from the package price first.</summary>
    public bool IsDelivery { get; set; }
    /// <summary>Banquet menu: only products with a banquet price, listed at that price.</summary>
    public bool IsBanquet { get; set; }
}

using Application.Orders.Services;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Tests;

public class SetSelectionRulesTests
{
    private const int Desserts = 10, Drinks = 20;

    private static MenuItem Item(int id, string name, int categoryId, bool isSet = false) => new()
    {
        Id = id, Name = name, MenuCategoryId = categoryId, IsSet = isSet,
        MenuCategory = new MenuCategory { Id = categoryId, Name = categoryId == Desserts ? "Desert" : "İçki" },
    };

    private static MenuItemSetComponent Component(MenuItem item, int? categoryMaxPick) => new()
    {
        ComponentMenuItemId = item.Id, ComponentMenuItem = item, Quantity = 1, CategoryMaxPick = categoryMaxPick,
    };

    private static OrderLine Child(int id, int parentId, MenuItem item, int quantity) => new()
    {
        Id = id, ParentLineId = parentId, MenuItemId = item.Id, MenuItem = item, Quantity = quantity, Status = OrderLineStatus.Pending,
    };

    // A set of 3 desserts (pick at most 1) and 1 coffee, ordered once.
    private readonly MenuItem _set = Item(1, "Desert seti", 5, isSet: true);
    private readonly MenuItem _tiramisu = Item(2, "Tiramisu", Desserts);
    private readonly MenuItem _cake = Item(3, "Tort", Desserts);
    private readonly MenuItem _pie = Item(4, "Pirog", Desserts);
    private readonly MenuItem _coffee = Item(5, "Kofe", Drinks);

    private List<MenuItemSetComponent> Components() => new()
    {
        Component(_tiramisu, 1), Component(_cake, 1), Component(_pie, 1), Component(_coffee, null),
    };

    private OrderLine SetLine(int quantity = 1) => new() { Id = 100, MenuItemId = _set.Id, MenuItem = _set, Quantity = quantity };

    [Fact]
    public void One_dessert_and_the_coffee_is_within_the_cap()
    {
        var lines = new List<OrderLine> { Child(1, 100, _tiramisu, 1), Child(2, 100, _coffee, 1) };

        Assert.Null(SetSelectionRules.FindCapViolation(SetLine(), lines, Components()));
    }

    [Fact]
    public void Two_desserts_break_a_cap_of_one()
    {
        var lines = new List<OrderLine> { Child(1, 100, _tiramisu, 1), Child(2, 100, _cake, 1), Child(3, 100, _coffee, 1) };

        var message = SetSelectionRules.FindCapViolation(SetLine(), lines, Components());

        Assert.NotNull(message);
        Assert.Contains("Desert", message);
    }

    [Fact]
    public void The_cap_grows_with_the_number_of_sets()
    {
        var lines = new List<OrderLine> { Child(1, 100, _tiramisu, 1), Child(2, 100, _cake, 1) };

        Assert.Null(SetSelectionRules.FindCapViolation(SetLine(quantity: 2), lines, Components()));
    }

    [Fact]
    public void A_category_without_a_cap_is_never_limited()
    {
        var lines = new List<OrderLine> { Child(1, 100, _coffee, 5) };

        Assert.Null(SetSelectionRules.FindCapViolation(SetLine(), lines, Components()));
    }

    [Fact]
    public void Raising_a_component_is_tested_with_its_new_quantity()
    {
        var lines = new List<OrderLine> { Child(1, 100, _tiramisu, 1) };

        Assert.NotNull(SetSelectionRules.FindCapViolation(SetLine(), lines, Components(), overrideLineId: 1, overrideQuantity: 2));
        Assert.Null(SetSelectionRules.FindCapViolation(SetLine(), lines, Components(), overrideLineId: 1, overrideQuantity: 1));
    }

    [Fact]
    public void A_set_can_allow_two_of_three_desserts()
    {
        var components = new List<MenuItemSetComponent> { Component(_tiramisu, 2), Component(_cake, 2), Component(_pie, 2) };
        var two = new List<OrderLine> { Child(1, 100, _tiramisu, 1), Child(2, 100, _cake, 1) };
        var three = new List<OrderLine> { Child(1, 100, _tiramisu, 1), Child(2, 100, _cake, 1), Child(3, 100, _pie, 1) };

        Assert.Null(SetSelectionRules.FindCapViolation(SetLine(), two, components));
        Assert.NotNull(SetSelectionRules.FindCapViolation(SetLine(), three, components));
    }
}

using Application.RestaurantTables;
using Xunit;

namespace Tests;

public class TableBulkPlannerTests
{
    [Fact]
    public void Numbering_starts_at_one()
        => Assert.Equal(new[] { "Masa 1", "Masa 2", "Masa 3" }, TableBulkPlanner.Names(Array.Empty<string>(), "Masa", 3));

    [Fact]
    public void Numbering_continues_after_the_highest_used_number()
        => Assert.Equal(
            new[] { "Masa 11", "Masa 12" },
            TableBulkPlanner.Names(new[] { "Masa 2", "masa 10", "VIP 50", "Masa A" }, " Masa ", 2));

    [Fact]
    public void A_given_start_skips_taken_names()
        => Assert.Equal(new[] { "1", "3", "4" }, TableBulkPlanner.Names(new[] { "2" }, "", 3, 1));

    [Fact]
    public void Names_are_ordered_as_people_count()
    {
        var names = new[] { "Masa 10", "Masa 2", "10", "9", "Masa 1" };

        var ordered = names.OrderBy(n => n, NaturalNameComparer.Instance).ToArray();

        Assert.Equal(new[] { "9", "10", "Masa 1", "Masa 2", "Masa 10" }, ordered);
    }

    [Fact]
    public void New_tables_go_in_rows_under_the_existing_ones()
    {
        var top = TableBulkPlanner.TopBelow(new[] { (0, 80), (200, 100) });

        Assert.Equal(320, top);
        Assert.Equal((20, 320), TableBulkPlanner.Position(0, top));
        Assert.Equal((920, 320), TableBulkPlanner.Position(9, top));
        Assert.Equal((20, 420), TableBulkPlanner.Position(10, top));
    }
}

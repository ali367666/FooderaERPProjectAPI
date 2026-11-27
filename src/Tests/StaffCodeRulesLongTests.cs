using Domain.Constants;
using Xunit;

namespace Tests;

public class StaffCodeRulesLongTests
{
    private static readonly string CardBarcode = new('7', 120);

    [Fact]
    public void A_store_code_can_be_a_long_card_barcode()
        => Assert.True(StaffCodeRules.IsValid(CardBarcode, allowLetters: true));

    [Fact]
    public void A_restaurant_code_keeps_its_20_digit_limit()
    {
        Assert.False(StaffCodeRules.IsValid(CardBarcode, allowLetters: false));
        Assert.True(StaffCodeRules.IsValid(new string('1', 20), allowLetters: false));
    }

    [Fact]
    public void Even_a_store_code_stops_at_the_column_limit()
        => Assert.False(StaffCodeRules.IsValid(new string('9', StaffCodeRules.StoreMaxLength + 1), allowLetters: true));
}

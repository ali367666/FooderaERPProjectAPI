using Domain.Constants;
using Xunit;

namespace Tests;

public class StaffCodeTests
{
    [Theory]
    [InlineData("1234", false, true)]
    [InlineData("12a4", false, false)]
    [InlineData("12a4", true, true)]
    [InlineData("Ali7", true, true)]
    [InlineData("al i", true, false)]
    [InlineData("ə12", true, false)]
    [InlineData("", true, false)]
    [InlineData("123456789012345678901", true, false)]
    public void Restaurants_keep_digits_only_and_stores_may_add_latin_letters(string code, bool allowLetters, bool expected)
        => Assert.Equal(expected, StaffCodeRules.IsValid(code, allowLetters));
}

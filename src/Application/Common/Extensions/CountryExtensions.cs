using Domain.Enums;

namespace Application.Common.Extensions;

public static class CountryExtensions
{
    public static string GetCode(this Country country) => country switch
    {
        Country.Azerbaijan => "AZE",
        Country.Turkey => "TUR",
        Country.Germany => "GER",
        Country.USA => "USA",
        _ => throw new ArgumentOutOfRangeException(nameof(country), "Unknown country")
    };

    public static string GetDisplayName(this Country country) => country switch
    {
        Country.Azerbaijan => "Azərbaycan",
        Country.Turkey => "Türkiyə",
        Country.Germany => "Almaniya",
        Country.USA => "ABŞ",
        _ => throw new ArgumentOutOfRangeException(nameof(country), "Unknown country")
    };
}
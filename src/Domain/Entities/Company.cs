using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Company : BaseEntity<int>
{
    public string CompanyCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public string? Address { get; set; }

    public string? TaxOfficeCode { get; set; }
    public string? TaxNumber { get; set; }

    public Country Country { get; set; }

    public string CountryCode { get; set; } = null!;

    public string? PrimaryPhoneNumber { get; set; }
    public string? SecondaryPhoneNumber { get; set; }

    public string? Email { get; set; }

    public ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();

}
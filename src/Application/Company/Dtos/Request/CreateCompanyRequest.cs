using Domain.Enums;

namespace Application.Company.Dtos.Request;

public class CreateCompanyRequest
{
    public string CompanyCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? TaxOfficeCode { get; set; }
    public string? TaxNumber { get; set; }
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
    public string? PrimaryPhoneNumber { get; set; }
    public string? SecondaryPhoneNumber { get; set; }
    public string? Email { get; set; }
    public Country Country { get; set; }

    // The company's own SuperAdmin account, created together with the company.
    public string OwnerFullName { get; set; } = default!;
    public string OwnerUserName { get; set; } = default!;
    public string OwnerEmail { get; set; } = default!;
    public string OwnerPassword { get; set; } = default!;
}
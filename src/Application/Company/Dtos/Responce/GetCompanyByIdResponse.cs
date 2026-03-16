namespace Application.Company.Dtos.Responce;

public class GetCompanyByIdResponse
{
    public int Id { get; set; }
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
}

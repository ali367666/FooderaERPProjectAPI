using Domain.Enums;

namespace Application.Company.Dtos.Request;

public class UpdateCompanyRequest
{
    public string? CompanyCode { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOfficeCode { get; set; }
    public Country? Country { get; set; }
}

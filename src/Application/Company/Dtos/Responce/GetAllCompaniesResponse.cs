namespace Application.Company.Dtos.Responce;

public class GetAllCompaniesResponse
{
    public int Id { get; set; }
    public string CompanyCode { get; set; } = default!;
    public string Name { get; set; } = default!;
}

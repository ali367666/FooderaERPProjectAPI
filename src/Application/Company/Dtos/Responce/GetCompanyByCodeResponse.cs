namespace Application.Company.Dtos.Responce;

public class GetCompanyByCodeResponse
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = default!;
    public List<RestaurantLookupItem> Restaurants { get; set; } = [];

    public class RestaurantLookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }
}

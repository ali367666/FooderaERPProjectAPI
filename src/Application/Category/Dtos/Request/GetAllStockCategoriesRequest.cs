namespace Application.StockCategory.Dtos.Request;

public class GetAllStockCategoriesRequest
{
    public int? CompanyId { get; set; }
    public bool? IsActive { get; set; }
    public int? ParentId { get; set; }
    public string? SearchTerm { get; set; }
}
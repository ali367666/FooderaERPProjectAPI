namespace Application.StockCategory.Dtos.Request;

public class CreateStockCategoryRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CompanyId { get; set; }
    public int? ParentId { get; set; }
}
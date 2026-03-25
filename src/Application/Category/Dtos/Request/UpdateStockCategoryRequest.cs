namespace Application.StockCategory.Dtos.Request;

public class UpdateStockCategoryRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int CompanyId { get; set; }
    public int? ParentId { get; set; }
}
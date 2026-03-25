namespace Application.StockCategory.Dtos.Response;

public class CreateStockCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int CompanyId { get; set; }
    public int? ParentId { get; set; }
}
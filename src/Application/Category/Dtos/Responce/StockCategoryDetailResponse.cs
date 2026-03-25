namespace Application.StockCategory.Dtos.Response;

public class StockCategoryDetailResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }

    public int? ParentId { get; set; }
    public string? ParentName { get; set; }

    public List<StockCategoryChildResponse> Children { get; set; } = new();
}
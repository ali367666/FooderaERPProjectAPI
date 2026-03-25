namespace Application.StockCategory.Dtos.Response;

public class StockCategoryChildResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
}
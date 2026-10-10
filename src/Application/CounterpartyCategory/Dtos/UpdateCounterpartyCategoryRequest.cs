namespace Application.CounterpartyCategory.Dtos;

public class UpdateCounterpartyCategoryRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

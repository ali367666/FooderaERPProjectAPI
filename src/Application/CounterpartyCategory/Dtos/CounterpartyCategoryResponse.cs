namespace Application.CounterpartyCategory.Dtos;

public class CounterpartyCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
}

namespace Application.CounterpartyCategory.Dtos;

public class CreateCounterpartyCategoryRequest
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

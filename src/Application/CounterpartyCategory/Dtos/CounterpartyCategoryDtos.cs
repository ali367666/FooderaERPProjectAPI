namespace Application.CounterpartyCategory.Dtos;

public class CounterpartyCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class CreateCounterpartyCategoryRequest
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class UpdateCounterpartyCategoryRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

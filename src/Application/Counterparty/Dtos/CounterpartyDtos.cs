namespace Application.Counterparty.Dtos;

public class CounterpartyResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public bool IsActive { get; set; }
    public decimal CurrentDebtAmount { get; set; }
}

public class CreateCounterpartyRequest
{
    public string Name { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCounterpartyRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AdjustCounterpartyDebtRequest
{
    public decimal NewDebtAmount { get; set; }
}

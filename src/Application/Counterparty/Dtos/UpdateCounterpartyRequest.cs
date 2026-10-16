namespace Application.Counterparty.Dtos;

public class UpdateCounterpartyRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

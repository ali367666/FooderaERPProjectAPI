namespace Application.Workstation.Dtos.Responce;

public class WorkstationResponse
{
    public int Id { get; set; }
    public int? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}

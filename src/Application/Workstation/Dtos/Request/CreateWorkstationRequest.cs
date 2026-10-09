namespace Application.Workstation.Dtos.Request;

public class CreateWorkstationRequest
{
    public int? RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

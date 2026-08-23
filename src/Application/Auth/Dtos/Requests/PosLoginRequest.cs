namespace Application.Auth.Dtos.Requests;

public sealed class PosLoginRequest
{
    public int CompanyId { get; set; }
    public int? RestaurantId { get; set; }
    public string? Code { get; set; }
    public string? RfidCardId { get; set; }
}

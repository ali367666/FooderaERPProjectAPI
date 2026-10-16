namespace Application.IdentityAdmin;

public class RoleListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? NormalizedName { get; set; }
    public int? CompanyId { get; set; }
    public bool RequiresRotatingPin { get; set; }
}

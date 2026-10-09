namespace Application.Orders.Dtos;

public class ReassignWaiterRequest
{
    public int NewEmployeeId { get; set; }

    /// <summary>POS code of the supervisor approving the change; not needed when the caller holds Pos.RedirectUser.</summary>
    public string? SupervisorCode { get; set; }
}

public class VerifyRedirectCodeRequest
{
    public string Code { get; set; } = default!;
}

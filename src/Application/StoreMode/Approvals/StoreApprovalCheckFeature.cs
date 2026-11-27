using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Constants;
using MediatR;

namespace Application.StoreMode.Approvals;

public class CheckStoreApprovalRequest
{
    /// <summary>What the cashier is about to do at the till: "RemoveLine" (take a product out of the cart) or "ClearCart".</summary>
    public string Action { get; set; } = default!;

    /// <summary>A short description for the log (e.g. the product name).</summary>
    public string? Detail { get; set; }

    public StoreApprovalRequest? Approval { get; set; }
}

public class CheckStoreApprovalResponse
{
    public bool Approved { get; set; }

    /// <summary>True when a manager approved it (false when the cashier may do it on their own).</summary>
    public bool ByManager { get; set; }
}

/// <summary>
/// Asks the server whether a step that only changes the cart on the screen is allowed. A cashier without the permission
/// needs a manager's username and password; the approval is logged. Nothing is changed besides that log entry.
/// </summary>
public record CheckStoreApprovalCommand(CheckStoreApprovalRequest Request) : IRequest<CheckStoreApprovalResponse>;

public class CheckStoreApprovalHandler : IRequestHandler<CheckStoreApprovalCommand, CheckStoreApprovalResponse>
{
    private readonly StoreApprovalService _approvals;
    private readonly IStoreRepository _repository;

    public CheckStoreApprovalHandler(StoreApprovalService approvals, IStoreRepository repository)
    {
        _approvals = approvals;
        _repository = repository;
    }

    public async Task<CheckStoreApprovalResponse> Handle(CheckStoreApprovalCommand command, CancellationToken ct)
    {
        var dto = command.Request;
        var (permission, what, action) = dto.Action switch
        {
            "ClearCart" => (AppPermissions.StoreSaleRemoveLine, "səbəti təmizləmək", "CartCleared"),
            _ => (AppPermissions.StoreSaleRemoveLine, "səbətdən məhsul silmək", "CartLineRemoved"),
        };

        var approver = await _approvals.RequireAsync(permission, dto.Approval, what, ct);
        if (approver is { } managerId)
        {
            await _approvals.LogAsync(action, "Cart", null, dto.Detail, managerId, dto.Approval?.Reason, ct);
            await _repository.SaveChangesAsync(ct);
        }

        return new CheckStoreApprovalResponse { Approved = true, ByManager = approver is not null };
    }
}

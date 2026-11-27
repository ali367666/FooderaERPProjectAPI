using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Constants;
using Domain.Entities.StoreMode;

namespace Application.StoreMode.Approvals;

/// <summary>
/// The manager-approval rule of the till: a step the cashier's own permission covers needs nothing; a step
/// the cashier lacks the permission for needs a manager who holds that same permission (typing their code), and the manager cannot be the cashier.
/// </summary>
public sealed class StoreApprovalService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityAdminService _identity;
    private readonly IStoreRepository _repository;

    private readonly Application.Common.Interfaces.Abstracts.Services.IStaffCodeResolver _staffCodes;

    public StoreApprovalService(ICurrentUserService currentUser, IIdentityAdminService identity, IStoreRepository repository, Application.Common.Interfaces.Abstracts.Services.IStaffCodeResolver staffCodes)
    {
        _staffCodes = staffCodes;
        _currentUser = currentUser;
        _identity = identity;
        _repository = repository;
    }

    /// <summary>
    /// Returns the approving manager's id, or null when the current user may do the step on their own.
    /// Throws when approval is needed and missing or wrong.
    /// </summary>
    public async Task<int?> RequireAsync(string ownPermission, StoreApprovalRequest? approval, string what, CancellationToken ct)
    {
        if (_currentUser.HasPermission(ownPermission))
            return null;

        if (approval is null)
            throw new BadRequestException($"Menecer təsdiqi lazımdır: {what}.");

        int? approverId;
        if (!string.IsNullOrWhiteSpace(approval.Code))
        {
            // The manager types only their own code. It is looked up the way the POS login does (rotating-PIN roles included).
            var manager = await _staffCodes.ResolveAsync(_currentUser.CompanyId, approval.Code.Trim(), ct);
            if (manager is null || !await _identity.UserHoldsPermissionAsync(manager.Id, _currentUser.CompanyId, ownPermission, ct))
                throw new BadRequestException("Kod yanlışdır və ya bu kodun sahibinin bu əməliyyata icazəsi yoxdur.");
            approverId = manager.Id;
        }
        else
        {
            var (id, error) = await _identity.VerifyApproverAsync(
                _currentUser.CompanyId, approval.UserName ?? string.Empty, approval.Password ?? string.Empty, ownPermission, ct);
            if (id is null)
                throw new BadRequestException(error ?? "Menecer təsdiqi alınmadı.");
            approverId = id;
        }
        if (approverId is { } resolved && resolved == _currentUser.UserId)
            throw new BadRequestException("Kassir öz əməliyyatını özü təsdiq edə bilməz; başqa menecer lazımdır.");

        return approverId;
    }

    /// <summary>Records an approval; saved together with the document it belongs to.</summary>
    public Task LogAsync(
        string action, string? documentType, int? documentId, string? documentNo, int approverId, string? reason, CancellationToken ct)
        => _repository.AddApprovalLogAsync(new StoreApprovalLog
        {
            CompanyId = _currentUser.CompanyId,
            Action = action,
            DocumentType = documentType,
            DocumentId = documentId,
            DocumentNo = documentNo,
            ApprovedByUserId = approverId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedByUserId = _currentUser.UserId,
        }, ct);
}

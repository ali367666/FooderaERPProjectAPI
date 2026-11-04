using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.StoreMode;
using Application.StoreMode.Approvals;
using Domain.Constants;
using NSubstitute;
using Xunit;

namespace Tests;

public class StoreApprovalCodeTests
{
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly IIdentityAdminService _identity = Substitute.For<IIdentityAdminService>();
    private readonly IStaffCodeResolver _codes = Substitute.For<IStaffCodeResolver>();
    private readonly StoreApprovalService _service;

    public StoreApprovalCodeTests()
    {
        _user.CompanyId.Returns(1);
        _user.UserId.Returns(7);
        _user.HasPermission(Arg.Any<string>()).Returns(false);
        _service = new StoreApprovalService(_user, _identity, Substitute.For<IStoreRepository>(), _codes);
    }

    [Fact]
    public async Task A_cashier_with_the_permission_needs_no_approval()
    {
        _user.HasPermission(AppPermissions.StoreSaleRemoveLine).Returns(true);
        Assert.Null(await _service.RequireAsync(AppPermissions.StoreSaleRemoveLine, null, "x", default));
    }

    [Fact]
    public async Task Without_the_permission_an_approval_is_required()
        => await Assert.ThrowsAsync<BadRequestException>(() => _service.RequireAsync(AppPermissions.StoreSaleRemoveLine, null, "x", default));

    [Fact]
    public async Task A_managers_code_alone_approves()
    {
        _codes.ResolveAsync(1, "M123", Arg.Any<CancellationToken>()).Returns(new Domain.Entities.User { Id = 20 });
        _identity.UserHoldsPermissionAsync(20, 1, AppPermissions.StoreSaleRemoveLine, Arg.Any<CancellationToken>()).Returns(true);

        var approver = await _service.RequireAsync(AppPermissions.StoreSaleRemoveLine, new StoreApprovalRequest { Code = "M123" }, "x", default);

        Assert.Equal(20, approver);
    }

    [Fact]
    public async Task A_code_without_the_approve_permission_is_refused()
    {
        _codes.ResolveAsync(1, "C1", Arg.Any<CancellationToken>()).Returns(new Domain.Entities.User { Id = 21 });
        _identity.UserHoldsPermissionAsync(21, 1, AppPermissions.StoreSaleRemoveLine, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RequireAsync(AppPermissions.StoreSaleRemoveLine, new StoreApprovalRequest { Code = "C1" }, "x", default));
    }

    [Fact]
    public async Task The_cashier_cannot_approve_with_their_own_code()
    {
        _codes.ResolveAsync(1, "SELF", Arg.Any<CancellationToken>()).Returns(new Domain.Entities.User { Id = 7 });
        _identity.UserHoldsPermissionAsync(7, 1, AppPermissions.StoreSaleRemoveLine, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RequireAsync(AppPermissions.StoreSaleRemoveLine, new StoreApprovalRequest { Code = "SELF" }, "x", default));
    }

    [Fact]
    public async Task An_unknown_code_is_refused()
        => await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RequireAsync(AppPermissions.StoreSaleRemoveLine, new StoreApprovalRequest { Code = "NOPE" }, "x", default));
}

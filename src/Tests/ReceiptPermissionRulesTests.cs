using Domain.Constants;
using Xunit;

namespace Tests;

public class ReceiptPermissionRulesTests
{
    [Fact]
    public void Seeing_receipts_opens_the_pos_receipts_screen()
    {
        var permissions = new HashSet<string> { AppPermissions.ReceiptView };

        ReceiptPermissionRules.Apply(permissions);

        Assert.Contains(AppPermissions.PosPrintOldReceipt, permissions);
        Assert.DoesNotContain(AppPermissions.ReceiptPrint, permissions);
    }

    [Fact]
    public void Without_seeing_receipts_the_screen_stays_closed()
    {
        var permissions = new HashSet<string> { AppPermissions.PosPrintOldReceipt, AppPermissions.ReceiptPrint };

        ReceiptPermissionRules.Apply(permissions);

        Assert.DoesNotContain(AppPermissions.PosPrintOldReceipt, permissions);
        Assert.Contains(AppPermissions.ReceiptPrint, permissions);
    }
}

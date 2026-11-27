namespace Domain.Constants;

public static class ReceiptPermissionRules
{
    /// <summary>
    /// The POS shows its receipts screen to holders of Pos.PrintOldReceipt, while the list on it needs Receipt.View. So that one permission
    /// opens the screen, Pos.PrintOldReceipt follows Receipt.View in the signed-in user's permissions, whatever is stored on the roles.
    /// </summary>
    public static void Apply(ISet<string> permissions)
    {
        if (permissions.Contains(AppPermissions.ReceiptView))
            permissions.Add(AppPermissions.PosPrintOldReceipt);
        else
            permissions.Remove(AppPermissions.PosPrintOldReceipt);
    }
}

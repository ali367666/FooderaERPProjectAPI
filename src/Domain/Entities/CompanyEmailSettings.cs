using Domain.Common;

namespace Domain.Entities;

/// <summary>The company's own Gmail account its e-mails (receipts and so on) are sent from. One row per company.</summary>
public class CompanyEmailSettings : CompanyEntity<int>
{
    public string Email { get; set; } = default!;
    /// <summary>The Gmail App Password, encrypted; it never leaves the server.</summary>
    public string ProtectedPassword { get; set; } = default!;
    public string? SenderName { get; set; }
}

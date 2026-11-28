namespace Application.CompanyEmail;

public class CompanyEmailSettingsDto
{
    public bool IsConfigured { get; set; }
    public string? Email { get; set; }
    public string? SenderName { get; set; }
}

public class SaveCompanyEmailSettingsRequest
{
    public string Email { get; set; } = default!;
    public string? SenderName { get; set; }
    /// <summary>The Gmail App Password. Empty keeps the saved one (only while the address stays the same).</summary>
    public string? AppPassword { get; set; }
}

public class SendTestEmailRequest
{
    public string To { get; set; } = default!;
}

/// <summary>
/// A company's own mail account. Deliberately called directly rather than through MediatR: the pipeline logs every request it
/// handles, and the password must not end up in a log.
/// </summary>
public interface ICompanyEmailService
{
    Task<CompanyEmailSettingsDto> GetAsync(int companyId, CancellationToken cancellationToken);
    Task<CompanyEmailSettingsDto> SaveAsync(int companyId, SaveCompanyEmailSettingsRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int companyId, CancellationToken cancellationToken);
    Task SendTestAsync(int companyId, string to, CancellationToken cancellationToken);
    Task<bool> IsConfiguredAsync(int companyId, CancellationToken cancellationToken);
}

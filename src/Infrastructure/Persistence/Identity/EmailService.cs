using Application.Common.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly CompanyEmailService _companyEmail;
    private readonly ICurrentUserService _currentUser;

    public EmailService(IOptions<EmailSettings> settings, CompanyEmailService companyEmail, ICurrentUserService currentUser)
    {
        _settings = settings.Value;
        _companyEmail = companyEmail;
        _currentUser = currentUser;
    }

    /// <summary>Sends from the current company's own Gmail when it has set one up, else from the server's mail account.</summary>
    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var settings = _currentUser.CompanyId > 0
            ? await _companyEmail.SmtpSettingsAsync(_currentUser.CompanyId, cancellationToken)
            : null;

        await SmtpMailer.SendAsync(settings ?? _settings, to, subject, htmlBody, cancellationToken);
    }
}

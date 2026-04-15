namespace Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(
        string subject,
        string to,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
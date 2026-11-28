using Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Infrastructure.Services;

internal static class SmtpMailer
{
    private const int TimeoutMilliseconds = 30_000;

    public static async Task SendAsync(
        EmailSettings settings, string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var smtp = new SmtpClient { Timeout = TimeoutMilliseconds };

        var secureOption = settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

        await smtp.ConnectAsync(settings.Host, settings.Port, secureOption, cancellationToken);
        await smtp.AuthenticateAsync(settings.UserName, settings.Password, cancellationToken);
        await smtp.SendAsync(email, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}

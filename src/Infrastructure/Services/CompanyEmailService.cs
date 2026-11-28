using System.Net.Mail;
using System.Security.Cryptography;
using Application.Common.Exceptions;
using Application.CompanyEmail;
using Domain.Entities;
using Infrastructure.Options;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class CompanyEmailService : ICompanyEmailService
{
    // Companies send through Gmail only: a free host field would let a tenant point the server at any address of its network.
    private const string GmailHost = "smtp.gmail.com";
    private const int GmailPort = 587;

    private readonly AppDbContext _db;
    private readonly IDataProtector _protector;
    private readonly ILogger<CompanyEmailService> _logger;

    public CompanyEmailService(AppDbContext db, IDataProtectionProvider protection, ILogger<CompanyEmailService> logger)
    {
        _db = db;
        _protector = protection.CreateProtector("CompanyEmail.AppPassword.v1");
        _logger = logger;
    }

    private Task<CompanyEmailSettings?> FindAsync(int companyId, CancellationToken ct)
        => _db.CompanyEmailSettings.FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);

    public async Task<CompanyEmailSettingsDto> GetAsync(int companyId, CancellationToken cancellationToken)
        => Map(await FindAsync(companyId, cancellationToken));

    public Task<bool> IsConfiguredAsync(int companyId, CancellationToken cancellationToken)
        => _db.CompanyEmailSettings.AnyAsync(x => x.CompanyId == companyId, cancellationToken);

    public async Task<CompanyEmailSettingsDto> SaveAsync(
        int companyId, SaveCompanyEmailSettingsRequest request, CancellationToken cancellationToken)
    {
        var email = ValidAddress(request.Email);
        var senderName = request.SenderName?.Replace("\r", " ").Replace("\n", " ").Trim();
        if (senderName?.Length > 100)
            throw new BadRequestException("Göndərən adı 100 simvoldan çox ola bilməz.");
        // Google shows the App Password in groups of four; the spaces are not part of it.
        var password = new string((request.AppPassword ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (password.Length > 100)
            throw new BadRequestException("App Password çox uzundur.");

        var row = await FindAsync(companyId, cancellationToken);
        if (password.Length == 0 && (row is null || !string.Equals(row.Email, email, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException("Bu ünvan üçün App Password yazın.");

        if (row is null)
        {
            row = new CompanyEmailSettings { CompanyId = companyId };
            _db.CompanyEmailSettings.Add(row);
        }

        row.Email = email;
        row.SenderName = string.IsNullOrEmpty(senderName) ? null : senderName;
        if (password.Length > 0)
            row.ProtectedPassword = _protector.Protect(password);

        await _db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    public async Task DeleteAsync(int companyId, CancellationToken cancellationToken)
    {
        if (await FindAsync(companyId, cancellationToken) is { } row)
        {
            _db.CompanyEmailSettings.Remove(row);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SendTestAsync(int companyId, string to, CancellationToken cancellationToken)
    {
        var recipient = ValidAddress(to);
        var settings = await SmtpSettingsAsync(companyId, cancellationToken)
            ?? throw new BadRequestException("Əvvəlcə Gmail ayarlarını yadda saxlayın.");

        try
        {
            await SmtpMailer.SendAsync(
                settings, recipient, "Foodera ERP — test məktubu",
                "<p>Bu test məktubudur. Poçt ayarlarınız işləyir.</p>", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Test məktubu göndərilmədi. CompanyId: {CompanyId}", companyId);
            throw new BadRequestException("Məktub göndərilmədi. Gmail ünvanını, App Password-u və internet bağlantısını yoxlayın.");
        }
    }

    /// <summary>The company's mail account ready for sending; null when it has none.</summary>
    internal async Task<EmailSettings?> SmtpSettingsAsync(int companyId, CancellationToken cancellationToken)
    {
        var row = await _db.CompanyEmailSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        if (row is null)
            return null;

        string password;
        try
        {
            password = _protector.Unprotect(row.ProtectedPassword);
        }
        catch (CryptographicException ex)
        {
            // The server's encryption keys changed (a new computer, a lost volume).
            _logger.LogError(ex, "Poçt parolu açılmadı. CompanyId: {CompanyId}", companyId);
            throw new BadRequestException("Saxlanmış poçt parolu oxunmadı. Tənzimləmələrdə App Password-u yenidən yazın.");
        }

        var name = row.SenderName
            ?? await _db.Companies.Where(c => c.Id == companyId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            ?? row.Email;

        return new EmailSettings
        {
            Host = GmailHost,
            Port = GmailPort,
            UseSsl = false,
            UserName = row.Email,
            Password = password,
            FromEmail = row.Email,
            FromName = name
        };
    }

    private static string ValidAddress(string? value)
    {
        var address = value?.Trim() ?? string.Empty;
        if (address.Length is 0 or > 256 || !MailAddress.TryCreate(address, out var parsed) || parsed.Address != address)
            throw new BadRequestException("E-poçt ünvanı yanlışdır.");
        return address;
    }

    private static CompanyEmailSettingsDto Map(CompanyEmailSettings? row) => new()
    {
        IsConfigured = row is not null,
        Email = row?.Email,
        SenderName = row?.SenderName
    };
}

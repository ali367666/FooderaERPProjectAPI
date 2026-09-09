using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Wolt/Bolt/189 platformlarının sifariş bildirişlərini qəbul edən webhook uc nöqtələri.
/// Hər restoran üçün Dashboard-da yaradılan DeliveryIntegration qeydinin ID-si URL-də göndərilir —
/// platforma qeydiyyat zamanı bu tam URL-i "webhook address" kimi veriləcək.
/// Hələlik yalnız gələn bildirişi doğrulayıb (mövcud olduqda) log-a yazır (bax: Seq) — daxili
/// Order/OrderLine-a çevrilmə real partnyor açarları və canlı test sifarişi əldə olunduqdan sonra əlavə olunacaq.
/// </summary>
[Route("api/delivery-webhooks")]
[ApiController]
[AllowAnonymous]
public class DeliveryWebhooksController : ControllerBase
{
    private readonly IDeliveryIntegrationRepository _repository;
    private readonly ILogger<DeliveryWebhooksController> _logger;

    public DeliveryWebhooksController(IDeliveryIntegrationRepository repository, ILogger<DeliveryWebhooksController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Sənəd: https://developer.wolt.com/docs/webhook — "WOLT-SIGNATURE" başlığı,
    /// HMAC-SHA256 (HEX), açar = DeliveryIntegration.WebhookSecret.
    /// </summary>
    [HttpPost("wolt/{integrationId:int}")]
    public async Task<IActionResult> Wolt(int integrationId, CancellationToken cancellationToken)
    {
        var rawBody = await ReadRawBodyAsync();

        var integration = await _repository.GetByIdForWebhookAsync(integrationId, cancellationToken);
        if (integration is null || integration.Provider != DeliveryProvider.Wolt || !integration.IsActive)
        {
            _logger.LogWarning("Wolt webhook: naməlum və ya aktiv olmayan integrationId={IntegrationId}", integrationId);
            return NotFound();
        }

        var signature = Request.Headers["WOLT-SIGNATURE"].FirstOrDefault();
        if (!VerifyHexHmacSha256(rawBody, integration.WebhookSecret, signature))
        {
            _logger.LogWarning("Wolt webhook: imza yoxlaması alınmadı. IntegrationId={IntegrationId}", integrationId);
            return Unauthorized();
        }

        _logger.LogInformation(
            "Wolt webhook alındı. IntegrationId={IntegrationId}, RestaurantId={RestaurantId}, Body={Body}",
            integrationId, integration.RestaurantId, rawBody);

        return Ok();
    }

    /// <summary>
    /// Qeyd: "x-external-integrator-id" / "x-server-authorization-hmac-sha256" başlıqları ictimai
    /// mənbələrdən götürülüb (Bolt-un rəsmi sənədi partnyor girişi tələb edir) — real test hesabı
    /// alınandan sonra dəqiqləşdirilməlidir.
    /// </summary>
    [HttpPost("bolt/{integrationId:int}")]
    public async Task<IActionResult> Bolt(int integrationId, CancellationToken cancellationToken)
    {
        var rawBody = await ReadRawBodyAsync();

        var integration = await _repository.GetByIdForWebhookAsync(integrationId, cancellationToken);
        if (integration is null || integration.Provider != DeliveryProvider.Bolt || !integration.IsActive)
        {
            _logger.LogWarning("Bolt webhook: naməlum və ya aktiv olmayan integrationId={IntegrationId}", integrationId);
            return NotFound();
        }

        var integratorId = Request.Headers["x-external-integrator-id"].FirstOrDefault();
        var signature = Request.Headers["x-server-authorization-hmac-sha256"].FirstOrDefault();

        if (!string.IsNullOrEmpty(integration.ApiKey) && integratorId != integration.ApiKey)
        {
            _logger.LogWarning("Bolt webhook: integrator ID uyğun gəlmir. IntegrationId={IntegrationId}", integrationId);
            return Unauthorized();
        }

        if (!VerifyBase64HmacSha256(rawBody, integration.WebhookSecret, signature))
        {
            _logger.LogWarning("Bolt webhook: imza yoxlaması alınmadı. IntegrationId={IntegrationId}", integrationId);
            return Unauthorized();
        }

        _logger.LogInformation(
            "Bolt webhook alındı. IntegrationId={IntegrationId}, RestaurantId={RestaurantId}, Body={Body}",
            integrationId, integration.RestaurantId, rawBody);

        return Ok();
    }

    /// <summary>
    /// 189 Delivery üçün ictimai API/webhook sənədi tapılmadı — imza yoxlaması yoxdur.
    /// Onlarla birbaşa əlaqə saxlanılıb sənəd əldə olunduqdan sonra tamamlanacaq.
    /// </summary>
    [HttpPost("189/{integrationId:int}")]
    public async Task<IActionResult> Delivery189(int integrationId, CancellationToken cancellationToken)
    {
        var rawBody = await ReadRawBodyAsync();

        var integration = await _repository.GetByIdForWebhookAsync(integrationId, cancellationToken);
        if (integration is null || integration.Provider != DeliveryProvider.Delivery189 || !integration.IsActive)
        {
            _logger.LogWarning("189 Delivery webhook: naməlum və ya aktiv olmayan integrationId={IntegrationId}", integrationId);
            return NotFound();
        }

        _logger.LogInformation(
            "189 Delivery webhook alındı (imza yoxlaması hələ yoxdur). IntegrationId={IntegrationId}, RestaurantId={RestaurantId}, Body={Body}",
            integrationId, integration.RestaurantId, rawBody);

        return Ok();
    }

    private async Task<string> ReadRawBodyAsync()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        return body;
    }

    private static bool VerifyHexHmacSha256(string payload, string? secret, string? signatureHex)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signatureHex)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedHex = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(signatureHex.ToLowerInvariant()));
    }

    private static bool VerifyBase64HmacSha256(string payload, string? secret, string? signatureBase64)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signatureBase64)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedBase64 = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedBase64),
            Encoding.UTF8.GetBytes(signatureBase64));
    }
}

using System.Text.Json;
using Application.Common.Interfaces;

namespace API.Middlewares;

/// <summary>
/// Online/Offline gate. A company without an active Online licence may only use the system from
/// its registered devices (X-Device-Key header); any other device gets 403 DEVICE_NOT_REGISTERED.
/// The SuperAdmin, anonymous endpoints and the licence/registration endpoints are never blocked.
/// </summary>
public class DeviceAccessMiddleware
{
    public const string DeviceKeyHeader = "X-Device-Key";

    private static readonly string[] OpenPrefixes =
    [
        "/api/auth",
        "/api/licenses/me",
        "/api/licenses/import",
        "/api/devices/register",
        "/api/company/lookup",
        "/api/company-settings/branding",
        "/api/public-menu",
        "/api/delivery-webhooks",
        "/swagger",
    ];

    private readonly RequestDelegate _next;

    public DeviceAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser, IDeviceAccessService access)
    {
        var path = context.Request.Path.Value ?? "";

        if (context.User.Identity?.IsAuthenticated == true
            && !currentUser.IsSuperAdmin
            && currentUser.CompanyId > 0
            && !OpenPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            var deviceKey = context.Request.Headers[DeviceKeyHeader].ToString();
            var decision = await access.CheckAsync(currentUser.CompanyId, deviceKey, context.RequestAborted);
            if (decision != AccessDecision.Allowed)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(decision == AccessDecision.LicenseRequired
                    ? new
                    {
                        success = false,
                        code = "LICENSE_REQUIRED",
                        message = "Lisenziya açarı yoxdur və ya vaxtı bitib. Yeni açarı daxil edin."
                    }
                    : new
                    {
                        success = false,
                        code = "DEVICE_NOT_REGISTERED",
                        message = "Bu cihazdan giriş icazəsi yoxdur. Sistemə yalnız qeydiyyatlı cihazlardan daxil olmaq olar (Online lisenziya aktiv deyil)."
                    }));
                return;
            }
        }

        await _next(context);
    }
}

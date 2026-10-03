using API.Middlewares;
using Application.License;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LicensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LicensesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The caller's company: Online/Offline state, days left, whether this device is registered.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<LicenseStatusResponse>> GetMine()
    {
        var deviceKey = Request.Headers[DeviceAccessMiddleware.DeviceKeyHeader].ToString();
        return Ok(await _mediator.Send(new GetMyLicenseStatusQuery(deviceKey)));
    }

    // ---- platform SuperAdmin only ----------------------------------------------------------

    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpGet("{companyId:int}")]
    public async Task<ActionResult<CompanyLicenseDetailResponse>> Get(int companyId) =>
        Ok(await _mediator.Send(new GetCompanyLicenseQuery(companyId)));

    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPut("{companyId:int}")]
    public async Task<ActionResult<CompanyLicenseDetailResponse>> Update(int companyId, [FromBody] UpdateCompanyLicenseRequest request) =>
        Ok(await _mediator.Send(new UpdateCompanyLicenseCommand(companyId, request)));

    /// <summary>"Ödənildi, uzat" — records the payment and extends Online.</summary>
    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPost("{companyId:int}/extend")]
    public async Task<ActionResult<CompanyLicenseDetailResponse>> Extend(int companyId, [FromBody] ExtendLicenseRequest request) =>
        Ok(await _mediator.Send(new ExtendCompanyLicenseCommand(companyId, request)));

    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPost("{companyId:int}/registration-code")]
    public async Task<ActionResult<DeviceRegistrationCodeResponse>> CreateRegistrationCode(int companyId) =>
        Ok(await _mediator.Send(new CreateDeviceRegistrationCodeCommand(companyId)));

    /// <summary>Signs a licence key for a Local installation (valid until the paid-until date).</summary>
    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPost("{companyId:int}/license-key")]
    public async Task<ActionResult<LicenseKeyResponse>> IssueKey(int companyId) =>
        Ok(await _mediator.Send(new IssueLicenseKeyCommand(companyId)));

    /// <summary>Local installation: paste the key received from the platform.</summary>
    [AllowAnonymous]
    [HttpPost("import")]
    public async Task<ActionResult<LicenseStatusResponse>> Import([FromBody] ImportLicenseKeyRequest request) =>
        Ok(await _mediator.Send(new ImportLicenseKeyCommand(request)));

    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpDelete("devices/{deviceId:int}")]
    public async Task<IActionResult> RevokeDevice(int deviceId)
    {
        await _mediator.Send(new RevokeTrustedDeviceCommand(deviceId));
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>A restaurant device exchanges the SuperAdmin's one-time code for its device key.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisterDeviceResponse>> Register([FromBody] RegisterDeviceRequest request) =>
        Ok(await _mediator.Send(new RegisterDeviceCommand(request)));
}

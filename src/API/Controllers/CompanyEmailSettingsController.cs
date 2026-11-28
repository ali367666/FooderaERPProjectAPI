using Application.Common.Interfaces;
using Application.Common.Responce;
using Application.CompanyEmail;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>The company's own Gmail account. The company always comes from the token (the SuperAdmin: the company filter).</summary>
[Route("api/company-settings/email")]
[ApiController]
[Authorize]
public class CompanyEmailSettingsController : ControllerBase
{
    private readonly ICompanyEmailService _email;
    private readonly ICurrentUserService _currentUser;

    public CompanyEmailSettingsController(ICompanyEmailService email, ICurrentUserService currentUser)
    {
        _email = email;
        _currentUser = currentUser;
    }

    /// <summary>The address and sender name; the password is never returned.</summary>
    [Authorize(Policy = AppPermissions.CompanySettingsView)]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(BaseResponse<CompanyEmailSettingsDto>.Ok(await _email.GetAsync(_currentUser.CompanyId, cancellationToken)));

    [Authorize(Policy = AppPermissions.CompanySettingsUpdate)]
    [HttpPut]
    public async Task<IActionResult> Save([FromBody] SaveCompanyEmailSettingsRequest request, CancellationToken cancellationToken)
        => Ok(BaseResponse<CompanyEmailSettingsDto>.Ok(
            await _email.SaveAsync(_currentUser.CompanyId, request, cancellationToken), "Poçt ayarları yadda saxlanıldı."));

    [Authorize(Policy = AppPermissions.CompanySettingsUpdate)]
    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await _email.DeleteAsync(_currentUser.CompanyId, cancellationToken);
        return Ok(BaseResponse.Ok("Poçt ayarları silindi."));
    }

    [Authorize(Policy = AppPermissions.CompanySettingsUpdate)]
    [HttpPost("test")]
    public async Task<IActionResult> SendTest([FromBody] SendTestEmailRequest request, CancellationToken cancellationToken)
    {
        await _email.SendTestAsync(_currentUser.CompanyId, request.To, cancellationToken);
        return Ok(BaseResponse.Ok("Test məktubu göndərildi."));
    }
}

using Application.CompanySettings.Commands.SetModules;
using Application.CompanySettings.Commands.Update;
using Application.CompanySettings.Dtos;
using Application.CompanySettings.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/company-settings")]
[ApiController]
[Authorize]
public class CompanySettingsController(IMediator mediator) : BaseController(mediator)
{
    [Authorize(Policy = AppPermissions.CompanySettingsView)]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? companyId, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetCompanySettingsQuery(companyId), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [AllowAnonymous]
    [HttpGet("branding")]
    public async Task<IActionResult> GetBranding(
        [FromQuery] int companyId,
        [FromQuery] int? restaurantId,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetCompanySettingsBrandingQuery(companyId, restaurantId), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.CompanySettingsUpdate)]
    [HttpPut]
    public async Task<IActionResult> Update(
        [FromQuery] int? companyId,
        [FromBody] UpdateCompanySettingsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new UpdateCompanySettingsCommand(request, companyId), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Bir müştəri şirkətinin aktiv modullarını (Filial, Anbar, Rezervasiya, Paket, Data Seçimi və s.)
    /// təyin edir. Yalnız platforma səviyyəsində şirkətləri idarə edən istifadəçi (Company.Update
    /// icazəsi) üçündür — müştərinin öz admin panelindən dəyişdirilə bilmir.
    /// </summary>
    /// <summary>
    /// SuperAdmin: whether a company's orders need the kitchen to accept them and mark them ready before payment (default yes).
    /// Off: the order only goes to the kitchen and the waiter may take the payment whenever they want.
    /// </summary>
    /// <summary>SuperAdmin: the payment banks of the catalog and which of them the company has (the admin cannot create or switch banks).</summary>
    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpGet("payment-banks")]
    public async Task<IActionResult> GetPaymentBanks([FromQuery] int companyId, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new Application.CompanySettings.Commands.SetPaymentBanks.GetCompanyPaymentBanksQuery(companyId), cancellationToken));

    /// <summary>SuperAdmin: gives the company exactly these banks (names from the catalog); the others are switched off.</summary>
    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPut("payment-banks")]
    public async Task<IActionResult> SetPaymentBanks([FromQuery] int companyId, [FromBody] List<string> names, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new Application.CompanySettings.Commands.SetPaymentBanks.SetCompanyPaymentBanksCommand(companyId, names), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>SuperAdmin: whether a company's authorised staff may correct paid receipts (payment method, items and quantities). Closed by default.</summary>
    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPut("receipt-edit")]
    public async Task<IActionResult> SetReceiptEdit([FromQuery] int companyId, [FromQuery] bool allowed, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new Application.CompanySettings.Commands.SetReceiptEdit.SetReceiptEditCommand(companyId, allowed), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPut("kitchen-approval")]
    public async Task<IActionResult> SetKitchenApproval([FromQuery] int companyId, [FromQuery] bool required, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new Application.CompanySettings.Commands.SetKitchenApproval.SetKitchenApprovalCommand(companyId, required), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPut("sales-lock")]
    public async Task<IActionResult> SetSalesLock([FromQuery] int companyId, [FromQuery] bool locked, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new Application.CompanySettings.Commands.SetSalesLock.SetSalesLockCommand(companyId, locked), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Bir müştəri şirkətinin aktiv modullarını (Filial, Anbar, Rezervasiya, Paket, Data Seçimi və s.)
    /// təyin edir. Yalnız platforma səviyyəsində şirkətləri idarə edən istifadəçi (Company.Update
    /// icazəsi) üçündür — müştərinin öz admin panelindən dəyişdirilə bilmir.
    /// </summary>
    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPut("modules")]
    public async Task<IActionResult> SetModules(
        [FromQuery] int companyId,
        [FromBody] SetCompanyModulesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new SetCompanyModulesCommand(companyId, request), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}

using Application.Printer.Commands;
using Application.Printer.Dtos;
using Application.Printer.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrintersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrintersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.PrinterView)]
    [HttpGet]
    public async Task<ActionResult<List<PrinterResponse>>> GetAll([FromQuery] int restaurantId)
    {
        var result = await _mediator.Send(new GetAllPrintersQuery(restaurantId));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PrinterCreate)]
    [HttpPost]
    public async Task<ActionResult<PrinterResponse>> Create([FromBody] CreatePrinterRequest request)
    {
        var result = await _mediator.Send(new CreatePrinterCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PrinterUpdate)]
    [HttpPut]
    public async Task<ActionResult<PrinterResponse>> Update([FromBody] UpdatePrinterRequest request)
    {
        var result = await _mediator.Send(new UpdatePrinterCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PrinterDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeletePrinterCommand(id));
        return Ok();
    }

    [Authorize(Policy = AppPermissions.PrinterPrint)]
    [HttpPost("{id:int}/print")]
    public async Task<IActionResult> Print(int id, [FromBody] PrintRequest request)
    {
        await _mediator.Send(new PrintToPrinterCommand(id, request.Content));
        return Ok();
    }
}

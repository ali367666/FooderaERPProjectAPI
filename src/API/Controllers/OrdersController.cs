using Application.Order.Dtos.Request;
using Application.Order.Commands.Serve;
using Application.Discounts.Commands.ApplyToOrder;
using Application.Discounts.Commands.RemoveFromOrder;
using Application.OrderLines.Commands.Add;
using Application.Orders.Commands.Pay;
using Application.Orders.Dtos.Request;
using Application.Orders.Commands.Cancel;
using Application.Orders.Commands.Complete;
using Application.OrderLines.Commands.Delete;
using Application.OrderLines.Commands.SetHold;
using Application.OrderLines.Commands.TimeBased;
using Application.OrderLines.Commands.Update;
using Application.Orders.Commands.Create;
using Application.Orders.Commands.Delete;
using Application.Orders.Commands.DiscardEmpty;
using Application.Orders.Commands.SetCounterparty;
using Application.Orders.Commands.SetOrderHold;
using Application.Orders.Commands.SetDeliveryDriver;
using Application.Orders.Commands.PrintKitchenTicket;
using Application.Orders.Commands.Bill;
using Application.Orders.Commands.Start;
using Application.Orders.Commands.Submit;
using Application.Orders.Commands.Update;
using Application.Orders.Commands.MoveTable;
using Application.Discounts.Commands.SetManual;
using Application.Orders.Commands.PayPart;
using Application.Orders.Commands.SetServiceCharge;
using Application.Orders.Commands.ReassignWaiter;
using Application.Orders.Queries.GetPayments;
using Application.Orders.Queries.VerifyRedirectCode;
using Application.Orders.Commands.TableRental;
using Application.Orders.Dtos;
using Application.Orders.Queries.GetAll;
using Application.Orders.Queries.GetById;
using Application.Orders.Queries.GetReceipt;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;
using Domain.Enums;

namespace FooderaERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.OrdersCreate)]
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _mediator.Send(new CreateOrderCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPut]
    public async Task<ActionResult<OrderResponse>> Update([FromBody] UpdateOrderRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosDeleteReceipt)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<string>> Delete(int id, [FromQuery] string? reason, [FromQuery] string? note)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id, reason, note));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("{id:int}/counterparty")]
    public async Task<ActionResult<OrderResponse>> SetCounterparty(int id, [FromQuery] int? counterpartyId)
    {
        var result = await _mediator.Send(new SetOrderCounterpartyCommand(id, counterpartyId));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PrinterPrint)]
    [HttpPost("{id:int}/print-kitchen")]
    public async Task<ActionResult<int>> PrintKitchenTicket(int id, [FromQuery] int printerId, [FromQuery] string? pin)
    {
        var result = await _mediator.Send(new PrintKitchenTicketCommand(id, printerId, pin));
        return Ok(result);
    }

    /// <summary>"Marş" — releases holds and tells the kitchen to prepare the order now.</summary>
    [Authorize(Policy = AppPermissions.PrinterPrint)]
    [HttpPost("{id:int}/mars")]
    public async Task<ActionResult<int>> SendMars(int id)
    {
        var result = await _mediator.Send(new SendMarsCommand(id));
        return Ok(result);
    }

    /// <summary>Customer bill printed before payment; locks the order when LockOrderAfterBill is on.</summary>
    [Authorize(Policy = AppPermissions.PosPrintReceipt)]
    [HttpPost("{id:int}/bill-printed")]
    public async Task<ActionResult<bool>> MarkBillPrinted(int id, [FromQuery] bool final = false)
    {
        var result = await _mediator.Send(new MarkBillPrintedCommand(id, final));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosUnlockBill)]
    [HttpPost("{id:int}/unlock-bill")]
    public async Task<IActionResult> UnlockBill(int id)
    {
        await _mediator.Send(new UnlockBillCommand(id));
        return NoContent();
    }

    [Authorize(Policy = AppPermissions.OrdersCreate)]
    [HttpPost("{id:int}/discard-empty")]
    public async Task<IActionResult> DiscardEmpty(int id)
    {
        await _mediator.Send(new DiscardEmptyOrderCommand(id));
        return NoContent();
    }
    [Authorize(Policy = AppPermissions.OrdersAdd)]
    [HttpPost("lines")]
    public async Task<ActionResult<OrderResponse>> AddLine([FromBody] AddOrderLineRequest request)
    {
        var result = await _mediator.Send(new AddOrderLineCommand(request));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("lines")]
    public async Task<ActionResult<OrderResponse>> UpdateLine([FromBody] UpdateOrderLineRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderLineCommand(request));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("lines/{id:int}/hold")]
    public async Task<ActionResult<OrderResponse>> SetLineHold(int id, [FromQuery] int? holdMinutes)
    {
        var result = await _mediator.Send(new SetOrderLineHoldCommand(id, holdMinutes));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("{id:int}/hold")]
    public async Task<ActionResult<OrderResponse>> SetOrderHold(int id, [FromQuery] bool hold)
    {
        var result = await _mediator.Send(new SetOrderHoldCommand(id, hold));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("{id:int}/delivery-driver")]
    public async Task<ActionResult<OrderResponse>> SetDeliveryDriver(int id, [FromQuery] int? driverEmployeeId)
    {
        var result = await _mediator.Send(new SetOrderDeliveryDriverCommand(id, driverEmployeeId));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("lines/{id:int}/start-timer")]
    public async Task<ActionResult<OrderResponse>> StartTimeBasedLine(int id)
    {
        var result = await _mediator.Send(new StartTimeBasedLineCommand(id));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("lines/{id:int}/stop-timer")]
    public async Task<ActionResult<OrderResponse>> StopTimeBasedLine(int id)
    {
        var result = await _mediator.Send(new StopTimeBasedLineCommand(id));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosDeleteProductInSale)]
    [HttpDelete("lines/{id}")]
    public async Task<ActionResult<OrderResponse>> DeleteLine(int id, [FromQuery] string? reason, [FromQuery] string? note)
    {
        var result = await _mediator.Send(new DeleteOrderLineCommand(id, reason, note));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("{id:int}/start-rental")]
    public async Task<ActionResult<OrderResponse>> StartTableRental(int id)
    {
        var result = await _mediator.Send(new StartTableRentalCommand(id));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.PosEditProductInSale)]
    [HttpPut("{id:int}/stop-rental")]
    public async Task<ActionResult<OrderResponse>> StopTableRental(int id)
    {
        var result = await _mediator.Send(new StopTableRentalCommand(id));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.OrdersView)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.OrdersView)]
    [HttpGet]
    public async Task<ActionResult<List<OrderResponse>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllOrdersQuery());
        return Ok(result);
    }

    // "OK" on the order screen: confirming an order and sending it to the kitchen needs no special permission.
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<OrderResponse>> Submit(int id)
    {
        var result = await _mediator.Send(new SubmitOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<OrderResponse>> Start(int id)
    {
        var result = await _mediator.Send(new StartOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<OrderResponse>> Complete(int id)
    {
        var result = await _mediator.Send(new CompleteOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosDeleteOrder)]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(int id, [FromQuery] string? reason, [FromQuery] string? note)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, reason, note));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersServe)]
    [HttpPut("{id}/serve")]
    public async Task<ActionResult<Application.Common.Responce.BaseResponse>> Serve(int id)
    {
        var result = await _mediator.Send(new ServeOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersPay)]
    [HttpPut("{id:int}/pay")]
    public async Task<ActionResult<OrderResponse>> Pay(int id, [FromBody] PayOrderRequest request)
    {
        var result = await _mediator.Send(new PayOrderCommand(id, request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersView)]
    [HttpGet("{id:int}/receipt")]
    public async Task<ActionResult<OrderReceiptResponse>> GetReceipt(
        int id, [FromQuery] int? paymentId = null, [FromQuery] bool? fiscal = null)
    {
        var result = await _mediator.Send(new GetOrderReceiptQuery(id, paymentId, fiscal));
        return Ok(result);
    }

    /// <summary>Part payments of the order and how much of each line they cover.</summary>
    [Authorize(Policy = AppPermissions.OrdersView)]
    [HttpGet("{id:int}/payments")]
    public async Task<ActionResult<OrderPaymentsResponse>> GetPayments(int id)
    {
        var result = await _mediator.Send(new GetOrderPaymentsQuery(id));
        return Ok(result);
    }

    /// <summary>"Hesab": one guest pays for the chosen items; the order closes with the last share.</summary>
    [Authorize(Policy = AppPermissions.OrdersPay)]
    [HttpPost("{id:int}/pay-part")]
    public async Task<ActionResult<PartPaymentResponse>> PayPart(int id, [FromBody] PayOrderPartRequest request)
    {
        var result = await _mediator.Send(new PayOrderPartCommand(id, request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.DiscountApply)]
    [HttpPost("{id:int}/apply-discount")]
    public async Task<ActionResult<OrderResponse>> ApplyDiscount(int id, [FromQuery] string code)
    {
        var result = await _mediator.Send(new ApplyDiscountToOrderCommand { OrderId = id, Code = code });
        return Ok(result);
    }

    /// <summary>"Servis haqqı qeyd etmək": the table's service charge in manat (0 clears it).</summary>
    [Authorize(Policy = AppPermissions.PosTableServiceCharge)]
    [HttpPut("{id:int}/service-charge")]
    public async Task<ActionResult<decimal>> SetServiceCharge(int id, [FromQuery] decimal amount)
    {
        var result = await _mediator.Send(new SetOrderServiceChargeCommand(id, amount));
        return Ok(result);
    }

    /// <summary>"₼" / "%" buttons: a hand-typed discount instead of a code. Returns the discount in manat.</summary>
    [Authorize(Policy = AppPermissions.DiscountApply)]
    [HttpPost("{id:int}/manual-discount")]
    public async Task<ActionResult<decimal>> SetManualDiscount(
        int id, [FromQuery] DiscountType type, [FromQuery] decimal value)
    {
        var result = await _mediator.Send(new SetManualDiscountCommand(id, type, value));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.DiscountApply)]
    [HttpPost("{id:int}/remove-discount")]
    public async Task<ActionResult<OrderResponse>> RemoveDiscount(int id)
    {
        var result = await _mediator.Send(new RemoveDiscountFromOrderCommand { OrderId = id });
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosMoveTable)]
    [HttpPut("{id:int}/move-table")]
    public async Task<ActionResult<OrderResponse>> MoveTable(int id, [FromQuery] int newTableId)
    {
        var result = await _mediator.Send(new MoveOrderTableCommand(id, newTableId));
        return Ok(result);
    }

    // Needs Pos.ChangeWaiter; the approval is checked inside — Pos.RedirectUser, or a supervisor's code.
    [Authorize(Policy = AppPermissions.PosChangeWaiter)]
    [HttpPut("{id:int}/reassign-waiter")]
    public async Task<ActionResult<OrderResponse>> ReassignWaiter(int id, [FromBody] ReassignWaiterRequest request)
    {
        var result = await _mediator.Send(new ReassignOrderWaiterCommand(id, request.NewEmployeeId, request.SupervisorCode));
        return Ok(result);
    }

    /// <summary>Early check of a supervisor's code so the POS can fail before the waiter picks an order.</summary>
    [Authorize(Policy = AppPermissions.PosChangeWaiter)]
    [HttpPost("verify-redirect-code")]
    public async Task<ActionResult<string>> VerifyRedirectCode([FromBody] VerifyRedirectCodeRequest request)
    {
        var approver = await _mediator.Send(new VerifyRedirectCodeQuery(request.Code));
        return Ok(approver);
    }
}
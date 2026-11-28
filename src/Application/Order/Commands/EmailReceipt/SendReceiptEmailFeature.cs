using System.Globalization;
using System.Net;
using System.Text;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.CompanyEmail;
using Application.Orders.Dtos;
using Application.Orders.Queries.GetReceipt;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Orders.Commands.EmailReceipt;

public class SendReceiptEmailRequest
{
    public string Email { get; set; } = default!;
}

public record SendReceiptEmailCommand(int OrderId, SendReceiptEmailRequest Request) : IRequest<Unit>;

public class SendReceiptEmailCommandValidator : AbstractValidator<SendReceiptEmailCommand>
{
    public SendReceiptEmailCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("E-poçt ünvanını yazın.")
            .EmailAddress().WithMessage("E-poçt ünvanı yanlışdır.")
            .MaximumLength(256).WithMessage("E-poçt ünvanı çox uzundur.");
    }
}

public class SendReceiptEmailCommandHandler : IRequestHandler<SendReceiptEmailCommand, Unit>
{
    private readonly IMediator _mediator;
    private readonly IEmailService _email;
    private readonly ILogger<SendReceiptEmailCommandHandler> _logger;

    private readonly ICompanyEmailService _companyEmail;
    private readonly ICurrentUserService _currentUser;

    public SendReceiptEmailCommandHandler(
        IMediator mediator, IEmailService email, ICompanyEmailService companyEmail, ICurrentUserService currentUser,
        ILogger<SendReceiptEmailCommandHandler> logger)
    {
        _companyEmail = companyEmail;
        _currentUser = currentUser;
        _mediator = mediator;
        _email = email;
        _logger = logger;
    }

    public async Task<Unit> Handle(SendReceiptEmailCommand command, CancellationToken ct)
    {
        // The receipt query only returns orders of the caller's own company.
        var receipt = await _mediator.Send(new GetOrderReceiptQuery(command.OrderId, null, null), ct);
        if (receipt.PaidAt is null)
            throw new BadRequestException("Yalnız ödənilmiş qəbz göndərilə bilər.");
        // A receipt goes out under the company's own address, never the server's.
        if (!await _companyEmail.IsConfiguredAsync(_currentUser.CompanyId, ct))
            throw new BadRequestException("Poçt ayarlanmayıb. Tənzimləmələr → E-poçt bölməsində Gmail ünvanını yazın.");

        try
        {
            await _email.SendAsync(
                command.Request.Email.Trim(),
                $"Qəbz {receipt.ReceiptNumber} — {receipt.RestaurantName}",
                Html(receipt),
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not BadRequestException)
        {
            _logger.LogError(ex, "Qəbz e-poçtla göndərilmədi. OrderId: {OrderId}", command.OrderId);
            throw new BadRequestException("E-poçt göndərilmədi. Poçt ayarlarını yoxlayın.");
        }

        return Unit.Value;
    }

    private static string Html(OrderReceiptResponse r)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture) + " AZN";

        var paidAt = BusinessTime.FromUtc(r.PaidAt!.Value).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        var html = new StringBuilder();
        html.Append("<div style=\"font-family:Arial,sans-serif;max-width:420px;margin:0 auto\">");
        html.Append($"<h2 style=\"margin:0\">{E(r.RestaurantName)}</h2><p style=\"color:#666;margin:4px 0\">{E(r.RestaurantAddress)}</p>");
        html.Append($"<p>Qəbz: <b>{E(r.ReceiptNumber)}</b><br>Tarix: {paidAt}<br>Masa: {E(r.TableName)}<br>Ofisiant: {E(r.WaiterName)}</p>");
        html.Append("<table style=\"width:100%;border-collapse:collapse\">");
        foreach (var line in r.Lines)
            html.Append($"<tr><td style=\"padding:4px 0;border-bottom:1px solid #eee\">{E(line.MenuItemName)}</td><td style=\"text-align:right;border-bottom:1px solid #eee\">{Money(line.LineTotal)}</td></tr>");

        void Row(string label, decimal value) => html.Append($"<tr><td>{label}</td><td style=\"text-align:right\">{Money(value)}</td></tr>");
        Row("Cəmi", r.TotalAmount);
        if (r.DiscountAmount > 0) Row("Endirim", -r.DiscountAmount);
        if (r.ServiceChargeAmount > 0) Row("Servis haqqı", r.ServiceChargeAmount);
        if (r.TableRentalAmount > 0) Row("Masa icarəsi", r.TableRentalAmount);
        if (r.IsFiscal) Row("ƏDV", r.VatAmount);
        html.Append($"<tr><td style=\"font-size:18px\"><b>Yekun</b></td><td style=\"text-align:right;font-size:18px\"><b>{Money(r.GrandTotal)}</b></td></tr>");
        html.Append("</table></div>");
        return html.ToString();
    }
}

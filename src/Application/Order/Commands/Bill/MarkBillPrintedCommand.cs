using System.Text;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Bill;

/// <summary>
/// The customer bill ("Hesab") was printed before payment. Returns whether the order is now locked.
/// <paramref name="Final"/>: the last print ("Qəbz çap et") — the order is locked whatever the company
/// setting says. The pre-check ("Müştəri qəbzi") never reaches this command, so it stays editable.
/// </summary>
public record MarkBillPrintedCommand(int OrderId, bool Final = false) : IRequest<bool>;

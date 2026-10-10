using System.Text;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Bill;

/// <summary>"Marş" — tell the kitchen to start now. Returns the number of kitchen printers that got the ticket.</summary>
public record SendMarsCommand(int OrderId) : IRequest<int>;

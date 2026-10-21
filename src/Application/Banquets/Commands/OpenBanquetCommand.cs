using Application.Banquets.Dtos;
using Application.Orders.Dtos;
using MediatR;

namespace Application.Banquets.Commands;

/// <summary>Opens a banquet order on the chosen tables of a hall: one bill, banquet menu and prices.</summary>
public record OpenBanquetCommand(OpenBanquetRequest Request) : IRequest<OrderResponse>;

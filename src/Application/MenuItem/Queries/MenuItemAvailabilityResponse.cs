using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.MenuItems.Queries.GetAvailability;

public class MenuItemAvailabilityResponse
{
    public List<int> OutOfStockMenuItemIds { get; set; } = new();
}

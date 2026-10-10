using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Queries.GetStockInfo;

public record GetMenuItemStockInfoQuery(int MenuItemId) : IRequest<MenuItemStockInfoResponse>;

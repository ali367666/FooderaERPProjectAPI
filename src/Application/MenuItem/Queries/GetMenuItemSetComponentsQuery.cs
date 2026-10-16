using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Queries.GetSetComponents;

public record GetMenuItemSetComponentsQuery(int SetMenuItemId) : IRequest<List<SetComponentResponse>>;

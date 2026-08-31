using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Queries.GetSetComponents;

public record GetMenuItemSetComponentsQuery(int SetMenuItemId) : IRequest<List<SetComponentResponse>>;

public class SetComponentResponse
{
    public int ComponentMenuItemId { get; set; }
    public string ComponentMenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
}

public class GetMenuItemSetComponentsQueryHandler : IRequestHandler<GetMenuItemSetComponentsQuery, List<SetComponentResponse>>
{
    private readonly IMenuItemSetComponentRepository _repository;

    public GetMenuItemSetComponentsQueryHandler(IMenuItemSetComponentRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SetComponentResponse>> Handle(GetMenuItemSetComponentsQuery request, CancellationToken cancellationToken)
    {
        var components = await _repository.GetBySetMenuItemIdAsync(request.SetMenuItemId, cancellationToken);
        return components.Select(c => new SetComponentResponse
        {
            ComponentMenuItemId = c.ComponentMenuItemId,
            ComponentMenuItemName = c.ComponentMenuItem.Name,
            Quantity = c.Quantity
        }).ToList();
    }
}

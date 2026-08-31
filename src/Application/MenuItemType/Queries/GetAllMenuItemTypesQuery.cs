using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItemType.Commands;
using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Queries;

public record GetAllMenuItemTypesQuery : IRequest<List<MenuItemTypeResponse>>;

public class GetAllMenuItemTypesQueryHandler : IRequestHandler<GetAllMenuItemTypesQuery, List<MenuItemTypeResponse>>
{
    private readonly IMenuItemTypeRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllMenuItemTypesQueryHandler(IMenuItemTypeRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<MenuItemTypeResponse>> Handle(GetAllMenuItemTypesQuery request, CancellationToken cancellationToken)
    {
        var itemTypes = await _repository.GetAllByCompanyAsync(_currentUserService.CompanyId, cancellationToken);
        return itemTypes.Select(CreateMenuItemTypeCommandHandler.Map).ToList();
    }
}

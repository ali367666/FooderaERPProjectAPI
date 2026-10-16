using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Commands;

public class CreateMenuItemTypeCommandHandler : IRequestHandler<CreateMenuItemTypeCommand, MenuItemTypeResponse>
{
    private readonly IMenuItemTypeRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateMenuItemTypeCommandHandler(IMenuItemTypeRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<MenuItemTypeResponse> Handle(CreateMenuItemTypeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var name = request.Request.Name.Trim();

        if (await _repository.ExistsByNameAsync(companyId, name, null, cancellationToken))
            throw new Exception("Bu adda məhsul növü artıq mövcuddur.");

        var itemType = new Domain.Entities.MenuItemType
        {
            CompanyId = companyId,
            Name = name,
            IsActive = request.Request.IsActive
        };

        await _repository.AddAsync(itemType, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(itemType);
    }

    internal static MenuItemTypeResponse Map(Domain.Entities.MenuItemType t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        IsActive = t.IsActive
    };
}

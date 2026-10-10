using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Commands;

public class UpdateMenuItemTypeCommandHandler : IRequestHandler<UpdateMenuItemTypeCommand, MenuItemTypeResponse>
{
    private readonly IMenuItemTypeRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMenuItemTypeCommandHandler(IMenuItemTypeRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<MenuItemTypeResponse> Handle(UpdateMenuItemTypeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var itemType = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (itemType is null)
            throw new Exception("Məhsul növü tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(companyId, name, itemType.Id, cancellationToken))
            throw new Exception("Bu adda məhsul növü artıq mövcuddur.");

        itemType.Name = name;
        itemType.IsActive = dto.IsActive;

        _repository.Update(itemType);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreateMenuItemTypeCommandHandler.Map(itemType);
    }
}

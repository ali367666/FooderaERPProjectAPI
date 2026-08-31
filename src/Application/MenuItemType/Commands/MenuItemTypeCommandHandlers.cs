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

public class DeleteMenuItemTypeCommandHandler : IRequestHandler<DeleteMenuItemTypeCommand>
{
    private readonly IMenuItemTypeRepository _repository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteMenuItemTypeCommandHandler(
        IMenuItemTypeRepository repository,
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteMenuItemTypeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var itemType = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (itemType is null)
            throw new Exception("Məhsul növü tapılmadı.");

        if (await _menuItemRepository.ExistsByItemTypeIdAsync(itemType.Id, cancellationToken))
            throw new Exception("Bu növü istifadə edən məhsul(lar) var — əvvəlcə onların növünü dəyişin.");

        _repository.Delete(itemType);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}

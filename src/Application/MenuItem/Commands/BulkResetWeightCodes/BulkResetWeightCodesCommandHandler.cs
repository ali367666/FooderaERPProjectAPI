using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.MenuItems.Commands.BulkResetWeightCodes;

public class BulkResetWeightCodesCommandHandler : IRequestHandler<BulkResetWeightCodesCommand, int>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public BulkResetWeightCodesCommandHandler(
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(BulkResetWeightCodesCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var items = await _menuItemRepository.GetAllAsync(companyId, cancellationToken);
        var weightBased = items
            .Where(x => x.UnitId == (int)UnitOfMeasure.Kg || x.UnitId == (int)UnitOfMeasure.Gram)
            .ToList();

        var now = DateTime.UtcNow;
        foreach (var item in weightBased)
        {
            item.WeightCode = $"{companyId}-{item.Id:D6}-{now:HHmmss}";
            _menuItemRepository.Update(item);
        }

        if (weightBased.Count > 0)
            await _menuItemRepository.SaveChangesAsync(cancellationToken);

        return weightBased.Count;
    }
}

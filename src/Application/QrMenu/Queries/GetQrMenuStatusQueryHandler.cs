using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PublicMenu.Services;
using Application.QrMenu.Dtos;
using MediatR;

namespace Application.QrMenu.Queries;

public class GetQrMenuStatusQueryHandler : IRequestHandler<GetQrMenuStatusQuery, QrMenuStatusResponse>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IQrMenuRepository _qrMenuRepository;
    private readonly IPublicMenuBuilder _menuBuilder;
    private readonly ICurrentUserService _currentUserService;

    public GetQrMenuStatusQueryHandler(
        IRestaurantRepository restaurantRepository,
        IQrMenuRepository qrMenuRepository,
        IPublicMenuBuilder menuBuilder,
        ICurrentUserService currentUserService)
    {
        _restaurantRepository = restaurantRepository;
        _qrMenuRepository = qrMenuRepository;
        _menuBuilder = menuBuilder;
        _currentUserService = currentUserService;
    }

    public async Task<QrMenuStatusResponse> Handle(GetQrMenuStatusQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null || restaurant.CompanyId != companyId)
            throw new NotFoundException("Filial tapılmadı.");

        // Every table gets its own token the first time the QR screen asks for it.
        var tables = await _qrMenuRepository.GetQrTablesAsync(companyId, request.RestaurantId, cancellationToken);
        var created = false;
        foreach (var table in tables.Where(t => string.IsNullOrEmpty(t.QrToken)))
        {
            table.QrToken = Guid.NewGuid().ToString("N");
            created = true;
        }
        if (created)
            await _qrMenuRepository.SaveChangesAsync(cancellationToken);

        var snapshot = await _qrMenuRepository.GetSnapshotAsync(request.RestaurantId, cancellationToken);
        var liveHash = _menuBuilder.ComputeHash(await _menuBuilder.BuildCategoriesAsync(companyId, cancellationToken));

        return new QrMenuStatusResponse
        {
            RestaurantId = request.RestaurantId,
            PublishedAtUtc = snapshot?.PublishedAtUtc,
            HasPendingChanges = snapshot is null || snapshot.ContentHash != liveHash,
            Tables = tables.Select(t => new QrMenuTableResponse
            {
                Id = t.Id,
                Name = t.Name,
                SectionId = t.SectionId,
                SectionName = t.Section?.Name,
                QrToken = t.QrToken!
            }).ToList()
        };
    }
}

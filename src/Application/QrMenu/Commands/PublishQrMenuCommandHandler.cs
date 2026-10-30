using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PublicMenu.Services;
using Application.QrMenu.Dtos;
using Application.QrMenu.Queries;
using Domain.Entities;
using MediatR;

namespace Application.QrMenu.Commands;

public class PublishQrMenuCommandHandler : IRequestHandler<PublishQrMenuCommand, QrMenuStatusResponse>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IQrMenuRepository _qrMenuRepository;
    private readonly IPublicMenuBuilder _menuBuilder;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public PublishQrMenuCommandHandler(
        IRestaurantRepository restaurantRepository,
        IQrMenuRepository qrMenuRepository,
        IPublicMenuBuilder menuBuilder,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _restaurantRepository = restaurantRepository;
        _qrMenuRepository = qrMenuRepository;
        _menuBuilder = menuBuilder;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<QrMenuStatusResponse> Handle(PublishQrMenuCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null || restaurant.CompanyId != companyId)
            throw new NotFoundException("Filial tapılmadı.");

        var categories = await _menuBuilder.BuildCategoriesAsync(companyId, cancellationToken);
        var json = JsonSerializer.Serialize(categories);
        var hash = _menuBuilder.ComputeHash(categories);

        var snapshot = await _qrMenuRepository.GetSnapshotAsync(request.RestaurantId, cancellationToken);
        if (snapshot is null)
        {
            snapshot = new PublicMenuSnapshot { CompanyId = companyId, RestaurantId = request.RestaurantId };
            await _qrMenuRepository.AddSnapshotAsync(snapshot, cancellationToken);
        }

        snapshot.MenuJson = json;
        snapshot.ContentHash = hash;
        snapshot.PublishedAtUtc = DateTime.UtcNow;
        await _qrMenuRepository.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetQrMenuStatusQuery(request.RestaurantId), cancellationToken);
    }
}

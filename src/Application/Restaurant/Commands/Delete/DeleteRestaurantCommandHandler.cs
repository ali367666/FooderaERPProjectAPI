using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Restaurant.Commands.Delete;

public sealed class DeleteRestaurantCommandHandler
    : IRequestHandler<DeleteRestaurantCommand, BaseResponse>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ILogger<DeleteRestaurantCommandHandler> _logger;

    public DeleteRestaurantCommandHandler(
        IRestaurantRepository restaurantRepository,
        ILogger<DeleteRestaurantCommandHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteRestaurantCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteRestaurantCommand başladı. RestaurantId: {RestaurantId}",
            request.Id);

        var restaurant = await _restaurantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (restaurant is null)
        {
            _logger.LogWarning(
                "Restaurant silinmədi. Restaurant tapılmadı. RestaurantId: {RestaurantId}",
                request.Id);

            return BaseResponse.Fail("Restaurant tapılmadı.");
        }

        _restaurantRepository.Delete(restaurant);
        await _restaurantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Restaurant uğurla silindi. RestaurantId: {RestaurantId}, Name: {Name}",
            restaurant.Id,
            restaurant.Name);

        return BaseResponse.Ok("Restaurant uğurla silindi.");
    }
}
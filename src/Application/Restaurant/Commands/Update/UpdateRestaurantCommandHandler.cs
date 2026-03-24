using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Restaurant.Commands.Update;

public sealed class UpdateRestaurantCommandHandler
    : IRequestHandler<UpdateRestaurantCommand, BaseResponse>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<UpdateRestaurantCommandHandler> _logger;

    public UpdateRestaurantCommandHandler(
        IRestaurantRepository restaurantRepository,
        ICompanyRepository companyRepository,
        ILogger<UpdateRestaurantCommandHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdateRestaurantCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        _logger.LogInformation(
            "UpdateRestaurantCommand başladı. RestaurantId: {RestaurantId}, Name: {Name}, CompanyId: {CompanyId}",
            request.Id,
            dto.Name,
            dto.CompanyId);

        var restaurant = await _restaurantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (restaurant is null)
        {
            _logger.LogWarning(
                "Restaurant update olunmadı. Restaurant tapılmadı. RestaurantId: {RestaurantId}",
                request.Id);

            return BaseResponse.Fail("Restaurant tapılmadı.");
        }

        var companyExists = await _companyRepository.ExistsAsync(dto.CompanyId, cancellationToken);
        if (!companyExists)
        {
            _logger.LogWarning(
                "Restaurant update olunmadı. Company tapılmadı. CompanyId: {CompanyId}",
                dto.CompanyId);

            return BaseResponse.Fail("Şirkət tapılmadı.");
        }

        var duplicateNameExists = await _restaurantRepository.ExistsAsync(
            x => x.Id != request.Id &&
                 x.CompanyId == dto.CompanyId &&
                 x.Name == dto.Name,
            cancellationToken);

        if (duplicateNameExists)
        {
            _logger.LogWarning(
                "Restaurant update olunmadı. Eyni adda restaurant artıq mövcuddur. RestaurantId: {RestaurantId}, Name: {Name}, CompanyId: {CompanyId}",
                request.Id,
                dto.Name,
                dto.CompanyId);

            return BaseResponse.Fail("Bu şirkət daxilində eyni adda restaurant artıq mövcuddur.");
        }

        restaurant.Name = dto.Name;
        restaurant.Description = dto.Description;
        restaurant.Address = dto.Address;
        restaurant.Phone = dto.Phone;
        restaurant.Email = dto.Email;
        restaurant.CompanyId = dto.CompanyId;

        _restaurantRepository.Update(restaurant);
        await _restaurantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Restaurant uğurla update olundu. RestaurantId: {RestaurantId}, Name: {Name}, CompanyId: {CompanyId}",
            restaurant.Id,
            restaurant.Name,
            restaurant.CompanyId);

        return BaseResponse.Ok("Restaurant uğurla yeniləndi.");
    }
}
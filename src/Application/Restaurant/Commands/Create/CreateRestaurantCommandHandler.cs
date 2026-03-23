using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Restaurant.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Restaurant.Commands.Create;

public sealed class CreateRestaurantCommandHandler
    : IRequestHandler<CreateRestaurantCommand, BaseResponse<CreateRestaurantResponse>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateRestaurantCommandHandler> _logger;

    public CreateRestaurantCommandHandler(
        IRestaurantRepository restaurantRepository,
        ICompanyRepository companyRepository,
        IMapper mapper,
        ILogger<CreateRestaurantCommandHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _companyRepository = companyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<CreateRestaurantResponse>> Handle(
        CreateRestaurantCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        _logger.LogInformation(
            "CreateRestaurantCommand başladı. Name: {Name}, CompanyId: {CompanyId}",
            dto.Name,
            dto.CompanyId);

        var companyExists = await _companyRepository.ExistsAsync(dto.CompanyId, cancellationToken);
        if (!companyExists)
        {
            _logger.LogWarning(
                "Restaurant yaradılmadı. Company tapılmadı. CompanyId: {CompanyId}",
                dto.CompanyId);

            return BaseResponse<CreateRestaurantResponse>.Fail("Şirkət tapılmadı.");
        }

        var restaurantNameExists = await _restaurantRepository.AnyAsync(
            x => x.CompanyId == dto.CompanyId && x.Name == dto.Name,
            cancellationToken);

        if (restaurantNameExists)
        {
            _logger.LogWarning(
                "Restaurant yaradılmadı. Eyni adda restaurant artıq mövcuddur. Name: {Name}, CompanyId: {CompanyId}",
                dto.Name,
                dto.CompanyId);

            return BaseResponse<CreateRestaurantResponse>.Fail("Bu şirkət daxilində eyni adda restaurant artıq mövcuddur.");
        }

        var restaurant = _mapper.Map<Domain.Entities.Restaurant>(dto);

        await _restaurantRepository.AddAsync(restaurant, cancellationToken);
        await _restaurantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Restaurant uğurla yaradıldı. RestaurantId: {RestaurantId}, Name: {Name}, CompanyId: {CompanyId}",
            restaurant.Id,
            restaurant.Name,
            restaurant.CompanyId);

        var response = new CreateRestaurantResponse
        {
            Id = restaurant.Id,
            Message = "Restaurant uğurla yaradıldı"
        };

        return BaseResponse<CreateRestaurantResponse>.Ok(response, "Restaurant uğurla yaradıldı");
    }
}
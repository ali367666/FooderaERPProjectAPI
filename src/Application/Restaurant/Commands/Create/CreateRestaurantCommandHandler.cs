using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateRestaurantCommandHandler> _logger;

    public CreateRestaurantCommandHandler(
        IRestaurantRepository restaurantRepository,
        ICompanyRepository companyRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IMapper mapper,
        ILogger<CreateRestaurantCommandHandler> logger)
    {
        _restaurantRepository = restaurantRepository;
        _companyRepository = companyRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
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

        var restaurantNameExists = await _restaurantRepository.ExistsAsync(
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

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Restaurant",
                    EntityId = restaurant.Id.ToString(),
                    ActionType = "Create",
                    Message = $"Restaurant yaradıldı. Id: {restaurant.Id}, Name: {restaurant.Name}, CompanyId: {restaurant.CompanyId}",
                    IsSuccess = true,
                    UserId = _currentUserService.UserId
                },
                cancellationToken);

            _logger.LogInformation(
                "Restaurant üçün audit log yazıldı. RestaurantId: {RestaurantId}",
                restaurant.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Restaurant create audit log yazılarkən xəta baş verdi. RestaurantId: {RestaurantId}",
                restaurant.Id);
        }

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
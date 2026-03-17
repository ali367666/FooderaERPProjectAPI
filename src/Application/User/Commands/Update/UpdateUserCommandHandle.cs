using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.User.Dtos.Responce;
using Domain.Enums;
using MediatR;

namespace Application.User.Commands.Update;

public class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, BaseResponse<UpdateUserResponseDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IIdentityService _identityService;

    public UpdateUserCommandHandler(
        ICompanyRepository companyRepository,
        IRestaurantRepository restaurantRepository,
        IIdentityService identityService)
    {
        _companyRepository = companyRepository;
        _restaurantRepository = restaurantRepository;
        _identityService = identityService;
    }

    public async Task<BaseResponse<UpdateUserResponseDto>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.dto;

        var user = await _identityService.GetUserByIdAsync(request.id);
        if (user is null)
            return BaseResponse<UpdateUserResponseDto>.Fail("User tapılmadı.");

        var company = await _companyRepository.GetByIdAsync(dto.CompanyId, cancellationToken);
        if (company is null)
            return BaseResponse<UpdateUserResponseDto>.Fail("Company tapılmadı.");

        string? restaurantName = null;
        int? restaurantId = null;

        if (dto.WorkplaceType == EmployeeWorkplaceType.Restaurant)
        {
            if (!dto.RestaurantId.HasValue)
                return BaseResponse<UpdateUserResponseDto>.Fail("RestaurantId göndərilməlidir.");

            var restaurant = await _restaurantRepository.GetByIdAsync(dto.RestaurantId.Value, cancellationToken);
            if (restaurant is null)
                return BaseResponse<UpdateUserResponseDto>.Fail("Restaurant tapılmadı.");

            if (restaurant.CompanyId != dto.CompanyId)
                return BaseResponse<UpdateUserResponseDto>.Fail("Seçilən restaurant bu company-ə aid deyil.");

            restaurantId = restaurant.Id;
            restaurantName = restaurant.Name;
        }

        var updateResult = await _identityService.UpdateUserAsync(
            request.id,
            dto.FullName,
            dto.WorkplaceType,
            dto.CompanyId,
            dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice ? null : restaurantId
        );

        if (!updateResult.Succeeded)
        {
            return new BaseResponse<UpdateUserResponseDto>
            {
                Success = false,
                Message = "User yenilənmədi.",
                Errors = updateResult.Errors
            };
        }

        var response = new UpdateUserResponseDto
        {
            Id = request.id,
            FullName = dto.FullName,
            WorkplaceType = dto.WorkplaceType,
            CompanyId = dto.CompanyId,
            CompanyName = company.Name,
            RestaurantId = dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice ? null : restaurantId,
            RestaurantName = restaurantName
        };

        return BaseResponse<UpdateUserResponseDto>.Ok(response, "User uğurla yeniləndi.");
    }
}
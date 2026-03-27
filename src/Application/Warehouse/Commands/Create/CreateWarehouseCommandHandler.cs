using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Commands.Create;

public class CreateWarehouseCommandHandler
    : IRequestHandler<CreateWarehouseCommand, BaseResponse<WarehouseResponse>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateWarehouseCommandHandler> _logger;

    public CreateWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        ICompanyRepository companyRepository,
        IRestaurantRepository restaurantRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<CreateWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _companyRepository = companyRepository;
        _restaurantRepository = restaurantRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<WarehouseResponse>> Handle(
        CreateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        _logger.LogInformation(
            "CreateWarehouseCommand started. CompanyId: {CompanyId}, Name: {Name}, Type: {Type}, RestaurantId: {RestaurantId}, DriverUserId: {DriverUserId}",
            dto.CompanyId,
            dto.Name,
            dto.Type,
            dto.RestaurantId,
            dto.DriverUserId);

        var companyExists = await _companyRepository.ExistsAsync(dto.CompanyId, cancellationToken);
        if (!companyExists)
        {
            _logger.LogWarning(
                "CreateWarehouseCommand failed. Company not found. CompanyId: {CompanyId}",
                dto.CompanyId);

            return BaseResponse<WarehouseResponse>.Fail("Company not found.");
        }

        var warehouseNameExists = await _warehouseRepository.ExistsByNameAsync(
            dto.Name.Trim(),
            dto.CompanyId,
            cancellationToken);

        if (warehouseNameExists)
        {
            _logger.LogWarning(
                "CreateWarehouseCommand failed. Warehouse name already exists. CompanyId: {CompanyId}, Name: {Name}",
                dto.CompanyId,
                dto.Name);

            return BaseResponse<WarehouseResponse>.Fail("Warehouse name already exists for this company.");
        }

        if (dto.Type == WarehouseType.Restaurant)
        {
            var restaurantExists = await _restaurantRepository.ExistsAsync(dto.RestaurantId!.Value, cancellationToken);
            if (!restaurantExists)
            {
                _logger.LogWarning(
                    "CreateWarehouseCommand failed. Restaurant not found. RestaurantId: {RestaurantId}",
                    dto.RestaurantId);

                return BaseResponse<WarehouseResponse>.Fail("Restaurant not found.");
            }
        }

        if (dto.Type == WarehouseType.Vehicle)
        {
            var driverExists = await _userRepository.ExistsAsync(dto.DriverUserId!.Value, cancellationToken);
            if (!driverExists)
            {
                _logger.LogWarning(
                    "CreateWarehouseCommand failed. Driver user not found. DriverUserId: {DriverUserId}",
                    dto.DriverUserId);

                return BaseResponse<WarehouseResponse>.Fail("Driver user not found.");
            }
        }

        Domain.Entities.Warehouse warehouse = new Domain.Entities.Warehouse
        {
            Name = dto.Name.Trim(),
            Type = dto.Type,
            CompanyId = dto.CompanyId,
            RestaurantId = dto.Type == WarehouseType.Restaurant ? dto.RestaurantId : null,
            DriverUserId = dto.Type == WarehouseType.Vehicle ? dto.DriverUserId : null
        };

        await _warehouseRepository.AddAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<WarehouseResponse>(warehouse);

        _logger.LogInformation(
            "CreateWarehouseCommand completed successfully. WarehouseId: {WarehouseId}, CompanyId: {CompanyId}",
            warehouse.Id,
            warehouse.CompanyId);

        return BaseResponse<WarehouseResponse>.Ok(response, "Warehouse created successfully.");
    }
}
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Commands.Update;

public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, BaseResponse>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateWarehouseCommandHandler> _logger;

    public UpdateWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        ICompanyRepository companyRepository,
        IRestaurantRepository restaurantRepository,
        IUserRepository userRepository,
        ILogger<UpdateWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _companyRepository = companyRepository;
        _restaurantRepository = restaurantRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateWarehouseCommand started. WarehouseId: {WarehouseId}, Name: {Name}, Type: {Type}, CompanyId: {CompanyId}, RestaurantId: {RestaurantId}, DriverUserId: {DriverUserId}",
            request.Id,
            request.Request.Name,
            request.Request.Type,
            request.Request.CompanyId,
            request.Request.RestaurantId,
            request.Request.DriverUserId);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouse is null)
        {
            _logger.LogWarning("Warehouse not found. WarehouseId: {WarehouseId}", request.Id);

            return new BaseResponse
            {
                Success = false,
                Message = "Warehouse not found."
            };
        }

        var company = await _companyRepository.GetByIdAsync(request.Request.CompanyId, cancellationToken);
        if (company is null)
        {
            _logger.LogWarning("Company not found. CompanyId: {CompanyId}", request.Request.CompanyId);

            return new BaseResponse
            {
                Success = false,
                Message = "Company not found."
            };
        }

        if (request.Request.RestaurantId.HasValue)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(request.Request.RestaurantId.Value, cancellationToken);

            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant not found. RestaurantId: {RestaurantId}", request.Request.RestaurantId.Value);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Restaurant not found."
                };
            }

            if (restaurant.CompanyId != request.Request.CompanyId)
            {
                _logger.LogWarning(
                    "Restaurant does not belong to the specified company. RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                    request.Request.RestaurantId.Value,
                    request.Request.CompanyId);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Restaurant does not belong to this company."
                };
            }
        }

        if (request.Request.DriverUserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(request.Request.DriverUserId.Value, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("Driver user not found. DriverUserId: {DriverUserId}", request.Request.DriverUserId.Value);

                return new BaseResponse
                {
                    Success = false,
                    Message = "Driver user not found."
                };
            }
        }

        var normalizedNewName = request.Request.Name.Trim();

        var sameNameExists = await _warehouseRepository.ExistsByNameAsync(
            normalizedNewName,
            request.Request.CompanyId,
            cancellationToken);

        if (sameNameExists &&
            !string.Equals(warehouse.Name, normalizedNewName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Another warehouse with same name already exists. WarehouseId: {WarehouseId}, Name: {Name}, CompanyId: {CompanyId}",
                request.Id,
                normalizedNewName,
                request.Request.CompanyId);

            return new BaseResponse
            {
                Success = false,
                Message = "Warehouse with this name already exists for the company."
            };
        }

        warehouse.Name = normalizedNewName;
        warehouse.Type = request.Request.Type;
        warehouse.CompanyId = request.Request.CompanyId;
        warehouse.RestaurantId = request.Request.RestaurantId;
        warehouse.DriverUserId = request.Request.DriverUserId;

        _warehouseRepository.Update(warehouse);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Warehouse updated successfully. WarehouseId: {WarehouseId}", warehouse.Id);

        return new BaseResponse
        {
            Success = true,
            Message = "Warehouse updated successfully."
        };
    }
}
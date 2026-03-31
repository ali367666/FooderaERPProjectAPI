using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Commands.Patch;

public class PatchWarehouseCommandHandler
    : IRequestHandler<PatchWarehouseCommand, BaseResponse>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ILogger<PatchWarehouseCommandHandler> _logger;

    public PatchWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        IRestaurantRepository restaurantRepository,
        ILogger<PatchWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _restaurantRepository = restaurantRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        PatchWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Patching warehouse. WarehouseId: {WarehouseId}",
            request.Id);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouse is null)
        {
            _logger.LogWarning(
                "PatchWarehouse failed. Warehouse not found. WarehouseId: {WarehouseId}",
                request.Id);

            return BaseResponse.Fail("Warehouse not found.");
        }

        // NAME
        if (!string.IsNullOrWhiteSpace(request.Request.Name))
            warehouse.Name = request.Request.Name;

        // TYPE
        if (request.Request.Type.HasValue)
            warehouse.Type = request.Request.Type.Value;

        // RESTAURANT
        if (request.Request.RestaurantId.HasValue)
        {
            if (request.Request.RestaurantId.Value == 0)
            {
                // detach etmək istəyirsənsə
                warehouse.RestaurantId = null;
            }
            else
            {
                var restaurant = await _restaurantRepository
                    .GetByIdAsync(request.Request.RestaurantId.Value, cancellationToken);

                if (restaurant is null)
                {
                    _logger.LogWarning(
                        "PatchWarehouse failed. Restaurant not found. RestaurantId: {RestaurantId}",
                        request.Request.RestaurantId.Value);

                    return BaseResponse.Fail("Restaurant not found.");
                }

                // Company check (çox vacibdir)
                if (restaurant.CompanyId != warehouse.CompanyId)
                {
                    _logger.LogWarning(
                        "PatchWarehouse failed. Restaurant does not belong to same company.");

                    return BaseResponse.Fail("Restaurant does not belong to this company.");
                }

                warehouse.RestaurantId = request.Request.RestaurantId.Value;
            }
        }

        // DRIVER
        if (request.Request.DriverUserId.HasValue)
        {
            if (request.Request.DriverUserId.Value == 0)
            {
                warehouse.DriverUserId = null;
            }
            else
            {
                // istəsən burada user yoxlanışı da əlavə edə bilərsən
                warehouse.DriverUserId = request.Request.DriverUserId.Value;
            }
        }

        await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Warehouse patched successfully. WarehouseId: {WarehouseId}",
            warehouse.Id);

        return BaseResponse.Ok("Warehouse updated successfully.");
    }
}

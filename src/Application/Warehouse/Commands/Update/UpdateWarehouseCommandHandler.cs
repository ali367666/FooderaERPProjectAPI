using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
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
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateWarehouseCommandHandler> _logger;

    public UpdateWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        ICompanyRepository companyRepository,
        IRestaurantRepository restaurantRepository,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IAuditLogService auditLogService,
        ILogger<UpdateWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _companyRepository = companyRepository;
        _restaurantRepository = restaurantRepository;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateWarehouseCommand started. WarehouseId: {WarehouseId}, Name: {Name}, Type: {Type}, CompanyId: {CompanyId}, RestaurantId: {RestaurantId}, ResponsibleEmployeeId: {ResponsibleEmployeeId}, DriverUserId: {DriverUserId}",
            request.Id,
            request.Request.Name,
            request.Request.Type,
            request.Request.CompanyId,
            request.Request.RestaurantId,
            request.Request.ResponsibleEmployeeId,
            request.Request.DriverUserId);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouse is null)
        {
            _logger.LogWarning("Warehouse not found. WarehouseId: {WarehouseId}", request.Id);
            return BaseResponse.Fail("Warehouse not found.");
        }

        var company = await _companyRepository.GetByIdAsync(request.Request.CompanyId, cancellationToken);
        if (company is null)
        {
            _logger.LogWarning("Company not found. CompanyId: {CompanyId}", request.Request.CompanyId);
            return BaseResponse.Fail("Company not found.");
        }

        if (request.Request.RestaurantId.HasValue)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(
                request.Request.RestaurantId.Value,
                cancellationToken);

            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant not found. RestaurantId: {RestaurantId}", request.Request.RestaurantId.Value);
                return BaseResponse.Fail("Restaurant not found.");
            }

            if (restaurant.CompanyId != request.Request.CompanyId)
            {
                _logger.LogWarning("Restaurant does not belong to company.");
                return BaseResponse.Fail("Restaurant does not belong to this company.");
            }
        }

        if (request.Request.DriverUserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(
                request.Request.DriverUserId.Value,
                cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("Driver user not found. DriverUserId: {DriverUserId}", request.Request.DriverUserId.Value);
                return BaseResponse.Fail("Driver user not found.");
            }
        }

        if (request.Request.ResponsibleEmployeeId.HasValue)
        {
            var responsible = await _employeeRepository.GetByIdAsync(
                request.Request.ResponsibleEmployeeId.Value,
                request.Request.CompanyId,
                cancellationToken);

            if (responsible is null)
            {
                _logger.LogWarning(
                    "Responsible employee not found. ResponsibleEmployeeId: {ResponsibleEmployeeId}, CompanyId: {CompanyId}",
                    request.Request.ResponsibleEmployeeId.Value,
                    request.Request.CompanyId);

                return BaseResponse.Fail("Responsible employee not found for this company.");
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
            _logger.LogWarning("Duplicate warehouse name detected.");
            return BaseResponse.Fail("Warehouse with this name already exists for the company.");
        }

        // 🔥 OLD VALUES (audit üçün)
        var oldName = warehouse.Name;
        var oldType = warehouse.Type;
        var oldCompanyId = warehouse.CompanyId;
        var oldRestaurantId = warehouse.RestaurantId;
        var oldDriverUserId = warehouse.DriverUserId;

        // UPDATE
        warehouse.Name = normalizedNewName;
        warehouse.Type = request.Request.Type;
        warehouse.CompanyId = request.Request.CompanyId;
        warehouse.RestaurantId = request.Request.RestaurantId;
        warehouse.ResponsibleEmployeeId = request.Request.ResponsibleEmployeeId;
        warehouse.DriverUserId = request.Request.DriverUserId;

        _warehouseRepository.Update(warehouse);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Warehouse",
                    EntityId = warehouse.Id.ToString(),
                    ActionType = "Update",
                    Message = $"Warehouse yeniləndi. Id: {warehouse.Id}, OldName: {oldName}, NewName: {warehouse.Name}, OldType: {oldType}, NewType: {warehouse.Type}, OldCompanyId: {oldCompanyId}, NewCompanyId: {warehouse.CompanyId}, OldRestaurantId: {oldRestaurantId}, NewRestaurantId: {warehouse.RestaurantId}, OldDriverUserId: {oldDriverUserId}, NewDriverUserId: {warehouse.DriverUserId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation("Warehouse audit log yazıldı. WarehouseId: {WarehouseId}", warehouse.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(auditEx,
                "Warehouse audit log yazılarkən xəta. WarehouseId: {WarehouseId}",
                warehouse.Id);
        }

        _logger.LogInformation("Warehouse updated successfully. WarehouseId: {WarehouseId}", warehouse.Id);

        return BaseResponse.Ok("Warehouse updated successfully.");
    }
}
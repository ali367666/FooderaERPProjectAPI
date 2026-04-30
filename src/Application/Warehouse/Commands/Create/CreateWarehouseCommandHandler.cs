using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using AutoMapper;
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
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateWarehouseCommandHandler> _logger;

    public CreateWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        ICompanyRepository companyRepository,
        IRestaurantRepository restaurantRepository,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IAuditLogService auditLogService,
        IMapper mapper,
        ILogger<CreateWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _companyRepository = companyRepository;
        _restaurantRepository = restaurantRepository;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _auditLogService = auditLogService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<WarehouseResponse>> Handle(
        CreateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        _logger.LogInformation(
            "CreateWarehouseCommand started. CompanyId: {CompanyId}, Name: {Name}, Type: {Type}, RestaurantId: {RestaurantId}, ResponsibleEmployeeId: {ResponsibleEmployeeId}, DriverUserId: {DriverUserId}",
            dto.CompanyId,
            dto.Name,
            dto.Type,
            dto.RestaurantId,
            dto.ResponsibleEmployeeId,
            dto.DriverUserId);

        var companyExists = await _companyRepository.ExistsAsync(dto.CompanyId, cancellationToken);
        if (!companyExists)
        {
            _logger.LogWarning(
                "CreateWarehouseCommand failed. Company not found. CompanyId: {CompanyId}",
                dto.CompanyId);

            return BaseResponse<WarehouseResponse>.Fail("Company not found.");
        }

        var trimmedName = dto.Name.Trim();

        var warehouseNameExists = await _warehouseRepository.ExistsByNameAsync(
            trimmedName,
            dto.CompanyId,
            cancellationToken);

        if (warehouseNameExists)
        {
            _logger.LogWarning(
                "CreateWarehouseCommand failed. Warehouse name already exists. CompanyId: {CompanyId}, Name: {Name}",
                dto.CompanyId,
                trimmedName);

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

        if (dto.ResponsibleEmployeeId.HasValue)
        {
            var responsible = await _employeeRepository.GetByIdAsync(
                dto.ResponsibleEmployeeId.Value,
                dto.CompanyId,
                cancellationToken);

            if (responsible is null)
            {
                _logger.LogWarning(
                    "CreateWarehouseCommand failed. Responsible employee not found. ResponsibleEmployeeId: {ResponsibleEmployeeId}, CompanyId: {CompanyId}",
                    dto.ResponsibleEmployeeId,
                    dto.CompanyId);

                return BaseResponse<WarehouseResponse>.Fail("Responsible employee not found for this company.");
            }
        }

        var warehouse = new Domain.Entities.Warehouse
        {
            Name = trimmedName,
            Type = dto.Type,
            CompanyId = dto.CompanyId,
            RestaurantId = dto.Type == WarehouseType.Restaurant ? dto.RestaurantId : null,
            ResponsibleEmployeeId = dto.ResponsibleEmployeeId,
            DriverUserId = dto.Type == WarehouseType.Vehicle ? dto.DriverUserId : null
        };

        await _warehouseRepository.AddAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Warehouse",
                    EntityId = warehouse.Id.ToString(),
                    ActionType = "Create",
                    Message = $"Warehouse yaradıldı. Id: {warehouse.Id}, Name: {warehouse.Name}, Type: {warehouse.Type}, CompanyId: {warehouse.CompanyId}, RestaurantId: {warehouse.RestaurantId}, DriverUserId: {warehouse.DriverUserId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "Warehouse üçün audit log yazıldı. WarehouseId: {WarehouseId}",
                warehouse.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Warehouse create audit log yazılarkən xəta baş verdi. WarehouseId: {WarehouseId}",
                warehouse.Id);
        }

        var response = _mapper.Map<WarehouseResponse>(warehouse);

        _logger.LogInformation(
            "CreateWarehouseCommand completed successfully. WarehouseId: {WarehouseId}, CompanyId: {CompanyId}",
            warehouse.Id,
            warehouse.CompanyId);

        return BaseResponse<WarehouseResponse>.Ok(response, "Warehouse created successfully.");
    }
}
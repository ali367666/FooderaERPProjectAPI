using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.RestaurantTables.Commands.EnsureStoreSaleTable;

public class EnsureStoreSaleTableCommandHandler
    : IRequestHandler<EnsureStoreSaleTableCommand, BaseResponse<int>>
{
    public const string StoreSaleTableName = "Mağaza Satışı";

    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly ICurrentUserService _currentUserService;

    public EnsureStoreSaleTableCommandHandler(
        IRestaurantTableRepository restaurantTableRepository,
        ICurrentUserService currentUserService)
    {
        _restaurantTableRepository = restaurantTableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<int>> Handle(EnsureStoreSaleTableCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var restaurantExists = await _restaurantTableRepository.RestaurantExistsAsync(
            companyId, request.RestaurantId, cancellationToken);
        if (!restaurantExists)
            return BaseResponse<int>.Fail("Restoran tapılmadı.");

        var tables = await _restaurantTableRepository.GetAllByRestaurantAsync(
            companyId, request.RestaurantId, cancellationToken);
        var existing = tables.FirstOrDefault(t => t.Name == StoreSaleTableName);
        if (existing is not null)
            return BaseResponse<int>.Ok(existing.Id);

        var table = new Domain.Entities.RestaurantTable
        {
            CompanyId = companyId,
            RestaurantId = request.RestaurantId,
            Name = StoreSaleTableName,
            Capacity = 1,
            IsActive = true,
            IsOccupied = false,
            Type = RestaurantTableType.Masa
        };

        await _restaurantTableRepository.AddAsync(table, cancellationToken);
        await _restaurantTableRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse<int>.Ok(table.Id);
    }
}

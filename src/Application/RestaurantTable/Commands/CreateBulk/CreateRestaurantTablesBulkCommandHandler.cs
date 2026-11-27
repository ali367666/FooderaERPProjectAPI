using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.RestaurantTables.Commands.CreateBulk;

public class CreateRestaurantTablesBulkCommandHandler
    : IRequestHandler<CreateRestaurantTablesBulkCommand, CreateRestaurantTablesBulkResponse>
{
    private readonly IRestaurantTableRepository _tables;
    private readonly IRestaurantSectionRepository _sections;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly ILogger<CreateRestaurantTablesBulkCommandHandler> _logger;

    public CreateRestaurantTablesBulkCommandHandler(
        IRestaurantTableRepository tables,
        IRestaurantSectionRepository sections,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        ILogger<CreateRestaurantTablesBulkCommandHandler> logger)
    {
        _tables = tables;
        _sections = sections;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<CreateRestaurantTablesBulkResponse> Handle(
        CreateRestaurantTablesBulkCommand command, CancellationToken cancellationToken)
    {
        var dto = command.Request;
        var companyId = _currentUser.CompanyId;
        var sectionId = dto.SectionId is > 0 ? dto.SectionId : null;

        if (!await _tables.RestaurantExistsAsync(companyId, dto.RestaurantId, cancellationToken))
            throw new BadRequestException("Filial tapılmadı.");

        if (sectionId is not null)
        {
            var section = await _sections.GetByIdAsync(sectionId.Value, companyId, cancellationToken);
            if (section is null || section.RestaurantId != dto.RestaurantId)
                throw new BadRequestException("Zal tapılmadı və ya bu filiala aid deyil.");
        }

        var existing = await _tables.GetAllByRestaurantAsync(companyId, dto.RestaurantId, cancellationToken);
        // A name is unique within its hall, so only that hall's names are taken.
        var names = TableBulkPlanner.Names(
            existing.Where(t => t.SectionId == sectionId).Select(t => t.Name), dto.Prefix, dto.Count, dto.StartNumber);
        var top = TableBulkPlanner.TopBelow(existing.Select(t => (t.PosY, t.Height)));

        for (var i = 0; i < names.Count; i++)
        {
            var (x, y) = TableBulkPlanner.Position(i, top);
            await _tables.AddAsync(new Domain.Entities.RestaurantTable
            {
                CompanyId = companyId,
                RestaurantId = dto.RestaurantId,
                SectionId = sectionId,
                Name = names[i],
                Capacity = dto.Capacity ?? 0,
                Type = Domain.Enums.RestaurantTableType.Masa,
                IsActive = true,
                IsOccupied = false,
                PosX = x,
                PosY = y,
                Width = TableBulkPlanner.TableSize,
                Height = TableBulkPlanner.TableSize
            }, cancellationToken);
        }

        // One save: either every table is created or none is.
        await _tables.SaveChangesAsync(cancellationToken);

        try
        {
            await _audit.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "RestaurantTable",
                    EntityId = dto.RestaurantId.ToString(),
                    ActionType = "Create",
                    Message = $"{names.Count} masa birdən yaradıldı. RestaurantId: {dto.RestaurantId}, {names[0]} - {names[^1]}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu masa yaradılmasının audit logu yazılmadı. RestaurantId: {RestaurantId}", dto.RestaurantId);
        }

        return new CreateRestaurantTablesBulkResponse(names.Count, names);
    }
}

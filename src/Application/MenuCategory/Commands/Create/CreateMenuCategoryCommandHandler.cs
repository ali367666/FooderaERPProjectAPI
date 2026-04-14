using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.MenuCategories.Commands.Create;

public class CreateMenuCategoryCommandHandler
    : IRequestHandler<CreateMenuCategoryCommand, int>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateMenuCategoryCommandHandler> _logger;

    public CreateMenuCategoryCommandHandler(
        IMenuCategoryRepository menuCategoryRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateMenuCategoryCommandHandler> logger)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<int> Handle(CreateMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "CreateMenuCategoryCommand başladı. CompanyId: {CompanyId}, Name: {Name}",
            companyId,
            request.Request.Name);

        if (companyId == 0)
        {
            _logger.LogWarning(
                "MenuCategory yaradılmadı. CompanyId tapılmadı.");

            throw new BadRequestException("CompanyId tapılmadı.");
        }

        var normalizedName = request.Request.Name.Trim();

        var exists = await _menuCategoryRepository.ExistsByNameAsync(
            companyId,
            normalizedName,
            cancellationToken);

        if (exists)
        {
            _logger.LogWarning(
                "MenuCategory yaradılmadı. Duplicate name. CompanyId: {CompanyId}, Name: {Name}",
                companyId,
                normalizedName);

            throw new BadRequestException("Bu adda menu kateqoriyası artıq mövcuddur.");
        }

        var entity = new MenuCategory
        {
            CompanyId = companyId,
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(request.Request.Description)
                ? null
                : request.Request.Description.Trim(),
            IsActive = true
        };

        await _menuCategoryRepository.AddAsync(entity, cancellationToken);
        await _menuCategoryRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "MenuCategory",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Create",
                    Message = $"MenuCategory yaradıldı. Id: {entity.Id}, Ad: {entity.Name}, CompanyId: {entity.CompanyId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "MenuCategory üçün audit log yazıldı. MenuCategoryId: {MenuCategoryId}",
                entity.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "MenuCategory create audit log yazılarkən xəta baş verdi. MenuCategoryId: {MenuCategoryId}",
                entity.Id);
        }

        _logger.LogInformation(
            "MenuCategory uğurla yaradıldı. MenuCategoryId: {MenuCategoryId}, CompanyId: {CompanyId}",
            entity.Id,
            entity.CompanyId);

        return entity.Id;
    }
}
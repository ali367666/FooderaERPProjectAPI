using Application.CashMovement.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.CashMovement.Commands;

public class CreateCashMovementCommandHandler : IRequestHandler<CreateCashMovementCommand, CashMovementResponse>
{
    private readonly ICashMovementRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateCashMovementCommandHandler(ICashMovementRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<CashMovementResponse> Handle(CreateCashMovementCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        if (dto.Amount <= 0)
            throw new Exception("Məbləğ 0-dan böyük olmalıdır.");

        var movement = new Domain.Entities.CashMovement
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            Type = dto.Type,
            Amount = dto.Amount,
            Reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),
            CreatedByUserId = _currentUserService.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(movement, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(movement);
    }

    internal static CashMovementResponse Map(Domain.Entities.CashMovement m) => new()
    {
        Id = m.Id,
        RestaurantId = m.RestaurantId,
        Type = m.Type,
        Amount = m.Amount,
        Reason = m.Reason,
        CreatedByUserId = m.CreatedByUserId,
        CreatedByUserName = m.CreatedByUser?.FullName,
        CreatedAtUtc = m.CreatedAtUtc
    };
}

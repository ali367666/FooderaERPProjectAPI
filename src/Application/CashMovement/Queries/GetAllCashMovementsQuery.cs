using Application.CashMovement.Commands;
using Application.CashMovement.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.CashMovement.Queries;

public record GetAllCashMovementsQuery(int RestaurantId, DateTime From, DateTime To) : IRequest<List<CashMovementResponse>>;

public class GetAllCashMovementsQueryHandler : IRequestHandler<GetAllCashMovementsQuery, List<CashMovementResponse>>
{
    private readonly ICashMovementRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllCashMovementsQueryHandler(ICashMovementRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<CashMovementResponse>> Handle(GetAllCashMovementsQuery request, CancellationToken cancellationToken)
    {
        var movements = await _repository.GetAllByRestaurantAsync(
            _currentUserService.CompanyId, request.RestaurantId, request.From, request.To, cancellationToken);
        return movements.Select(CreateCashMovementCommandHandler.Map).ToList();
    }
}

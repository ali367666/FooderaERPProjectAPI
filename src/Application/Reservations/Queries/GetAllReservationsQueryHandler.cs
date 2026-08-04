using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Queries;

public class GetAllReservationsQueryHandler
    : IRequestHandler<GetAllReservationsQuery, List<ReservationResponse>>
{
    private readonly IReservationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetAllReservationsQueryHandler(IReservationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<ReservationResponse>> Handle(GetAllReservationsQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.GetAllAsync(_currentUser.CompanyId, request.Date, cancellationToken);
        return list.Select(ReservationMapper.ToResponse).ToList();
    }
}

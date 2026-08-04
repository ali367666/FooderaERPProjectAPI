using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.Reservations.Commands.Delete;

public class DeleteReservationCommandHandler : IRequestHandler<DeleteReservationCommand>
{
    private readonly IReservationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public DeleteReservationCommandHandler(IReservationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteReservationCommand request, CancellationToken cancellationToken)
    {
        var r = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId, cancellationToken)
            ?? throw new Exception("Rezervasiya tapılmadı.");

        _repo.Remove(r);
        await _repo.SaveChangesAsync(cancellationToken);
    }
}

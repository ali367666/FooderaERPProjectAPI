using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.Discounts.Commands.Delete;

public class DeleteDiscountCommandHandler : IRequestHandler<DeleteDiscountCommand>
{
    private readonly IDiscountRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public DeleteDiscountCommandHandler(IDiscountRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteDiscountCommand request, CancellationToken cancellationToken)
    {
        var discount = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId, cancellationToken)
            ?? throw new Exception("Endirim tapılmadı.");

        _repo.Remove(discount);
        await _repo.SaveChangesAsync(cancellationToken);
    }
}

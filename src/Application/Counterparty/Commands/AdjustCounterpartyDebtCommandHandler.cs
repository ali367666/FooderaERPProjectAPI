using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public class AdjustCounterpartyDebtCommandHandler : IRequestHandler<AdjustCounterpartyDebtCommand, CounterpartyResponse>
{
    private readonly ICounterpartyRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public AdjustCounterpartyDebtCommandHandler(ICounterpartyRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<CounterpartyResponse> Handle(AdjustCounterpartyDebtCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var counterparty = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (counterparty is null)
            throw new Exception("Konturagent tapılmadı.");

        counterparty.CurrentDebtAmount = request.Request.NewDebtAmount;

        _repository.Update(counterparty);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreateCounterpartyCommandHandler.Map(counterparty);
    }
}

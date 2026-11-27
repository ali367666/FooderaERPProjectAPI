using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Queries;

public class GetPaymentBanksQueryHandler : IRequestHandler<GetPaymentBanksQuery, List<PaymentBankResponse>>
{
    private readonly ICounterpartyRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetPaymentBanksQueryHandler(ICounterpartyRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<PaymentBankResponse>> Handle(GetPaymentBanksQuery request, CancellationToken cancellationToken)
    {
        var counterparties = await _repository.GetAllByCompanyAsync(_currentUserService.CompanyId, cancellationToken);
        return counterparties
            .Where(c => c.IsBank && c.IsActive)
            .Select(c => new PaymentBankResponse { Id = c.Id, Name = c.Name })
            .ToList();
    }
}

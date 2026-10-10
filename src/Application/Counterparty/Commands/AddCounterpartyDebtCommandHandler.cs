using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Dtos;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Counterparty.Commands;

public class AddCounterpartyDebtCommandHandler : IRequestHandler<AddCounterpartyDebtCommand, CounterpartyResponse>
{
    private readonly ICounterpartyRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public AddCounterpartyDebtCommandHandler(ICounterpartyRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<CounterpartyResponse> Handle(AddCounterpartyDebtCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var counterparty = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken)
            ?? throw new Exception("Konturagent tapılmadı.");

        var amount = Math.Round(request.Request.Amount, 2, MidpointRounding.AwayFromZero);
        counterparty.CurrentDebtAmount += amount;

        await _repository.AddDebtEntryAsync(new CounterpartyDebtEntry
        {
            CompanyId = companyId,
            CounterpartyId = counterparty.Id,
            Type = CounterpartyDebtEntryType.Added,
            Amount = amount,
            BalanceAfter = counterparty.CurrentDebtAmount,
            Note = string.IsNullOrWhiteSpace(request.Request.Note) ? null : request.Request.Note.Trim(),
            CreatedByUserId = _currentUserService.UserId > 0 ? _currentUserService.UserId : null
        }, cancellationToken);

        _repository.Update(counterparty);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreateCounterpartyCommandHandler.Map(counterparty);
    }
}

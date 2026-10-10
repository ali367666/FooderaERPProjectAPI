using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Dtos;
using Domain.Entities;
using Domain.Enums;
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

        var newDebt = Math.Round(request.Request.NewDebtAmount, 2, MidpointRounding.AwayFromZero);
        var delta = newDebt - counterparty.CurrentDebtAmount;
        counterparty.CurrentDebtAmount = newDebt;

        if (delta != 0)
        {
            await _repository.AddDebtEntryAsync(new CounterpartyDebtEntry
            {
                CompanyId = companyId,
                CounterpartyId = counterparty.Id,
                Type = CounterpartyDebtEntryType.Adjusted,
                Amount = delta,
                BalanceAfter = newDebt,
                Note = string.IsNullOrWhiteSpace(request.Request.Note) ? null : request.Request.Note.Trim(),
                CreatedByUserId = _currentUserService.UserId > 0 ? _currentUserService.UserId : null
            }, cancellationToken);
        }

        _repository.Update(counterparty);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreateCounterpartyCommandHandler.Map(counterparty);
    }
}

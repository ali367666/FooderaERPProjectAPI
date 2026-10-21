using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;

namespace Application.Common.Helpers;

/// <summary>A card payment must name the bank it went through — one of the company's fixed payment banks.</summary>
public static class PaymentBankResolver
{
    public static async Task<Domain.Entities.Counterparty> RequireAsync(
        ICounterpartyRepository counterpartyRepository,
        int companyId,
        int? bankId,
        CancellationToken cancellationToken)
    {
        if (bankId is null or <= 0)
            throw new BadRequestException("Kart ödənişi üçün bankı seçin.");

        var bank = await counterpartyRepository.GetByIdAsync(bankId.Value, companyId, cancellationToken);
        if (bank is null || !bank.IsBank || !bank.IsActive)
            throw new BadRequestException("Seçilmiş bank tapılmadı.");

        return bank;
    }
}

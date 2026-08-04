namespace Application.Common.Interfaces.Abstracts.Services;

public interface ICbarExchangeRateService
{
    Task<decimal> GetRateAsync(string currencyCode, DateTime date, CancellationToken cancellationToken = default);
}

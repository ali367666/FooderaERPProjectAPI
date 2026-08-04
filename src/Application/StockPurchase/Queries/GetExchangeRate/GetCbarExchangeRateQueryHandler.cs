using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockPurchase.Queries.GetExchangeRate;

public class GetCbarExchangeRateQueryHandler
    : IRequestHandler<GetCbarExchangeRateQuery, BaseResponse<decimal>>
{
    private readonly ICbarExchangeRateService _cbarService;
    private readonly ILogger<GetCbarExchangeRateQueryHandler> _logger;

    public GetCbarExchangeRateQueryHandler(
        ICbarExchangeRateService cbarService,
        ILogger<GetCbarExchangeRateQueryHandler> logger)
    {
        _cbarService = cbarService;
        _logger = logger;
    }

    public async Task<BaseResponse<decimal>> Handle(
        GetCbarExchangeRateQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var rate = await _cbarService.GetRateAsync(
                request.CurrencyCode, request.Date, cancellationToken);

            return new BaseResponse<decimal> { Success = true, Data = rate };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CBAR məzənnə alınarkən xəta. Currency: {Currency}, Date: {Date}",
                request.CurrencyCode, request.Date);

            return new BaseResponse<decimal>
            {
                Success = false,
                Message = $"CBAR məzənnəsi alınmadı: {ex.Message}"
            };
        }
    }
}

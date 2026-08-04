using Application.Common.Interfaces.Abstracts.Services;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace Infrastructure.Services;

public class CbarExchangeRateService : ICbarExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CbarExchangeRateService> _logger;

    private readonly Dictionary<string, decimal> _cache = new();

    public CbarExchangeRateService(HttpClient httpClient, ILogger<CbarExchangeRateService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<decimal> GetRateAsync(
        string currencyCode,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(currencyCode, "AZN", StringComparison.OrdinalIgnoreCase))
            return 1m;

        // Bazar və bazar ertəsi günlər CBAR məzənnə dərc etmir — əvvəlki iş gününə keç
        var workDate = GetLastWorkday(date);

        var key = $"{currencyCode.ToUpper()}_{workDate:yyyyMMdd}";
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        // Maksimum 3 iş günü geri get (bayram günlərini keçmək üçün)
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var tryDate = workDate.AddDays(-attempt);
            // Bazar günlərini atla
            while (tryDate.DayOfWeek == DayOfWeek.Saturday || tryDate.DayOfWeek == DayOfWeek.Sunday)
                tryDate = tryDate.AddDays(-1);

            var dateStr = tryDate.ToString("dd.MM.yyyy");
            var url = $"https://www.cbar.az/currencies/{dateStr}.xml";

            _logger.LogInformation("CBAR sorğusu: {Url}", url);

            try
            {
                var xml = await _httpClient.GetStringAsync(url, cancellationToken);
                var doc = XDocument.Parse(xml);

                var valEl = doc.Descendants("Valute")
                    .FirstOrDefault(e =>
                        string.Equals((string?)e.Attribute("Code"), currencyCode, StringComparison.OrdinalIgnoreCase));

                if (valEl is null)
                {
                    _logger.LogWarning("CBAR: {Currency} {Date}-də tapılmadı, əvvəlki günə baxılır.", currencyCode, dateStr);
                    continue;
                }

                var valueStr = valEl.Element("Value")?.Value?.Replace(',', '.') ?? "0";
                if (!decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var rate) || rate <= 0)
                    continue;

                // Nominal nəzərə al (məs. 100 RUB üçün nominal=100)
                var nominalStr = valEl.Element("Nominal")?.Value ?? "1";
                if (decimal.TryParse(nominalStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var nominal) && nominal > 1)
                    rate = rate / nominal;

                _cache[key] = rate;
                _logger.LogInformation("CBAR: 1 {Currency} = {Rate} AZN ({Date})", currencyCode, rate, dateStr);
                return rate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CBAR sorğusu alınmadı: {Date}", dateStr);
            }
        }

        throw new InvalidOperationException(
            $"CBAR-dan '{currencyCode}' valyutasının məzənnəsi alına bilmədi ({date:dd.MM.yyyy} tarixi üçün).");
    }

    private static DateTime GetLastWorkday(DateTime date)
    {
        var d = date.Date;
        while (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
            d = d.AddDays(-1);
        return d;
    }
}

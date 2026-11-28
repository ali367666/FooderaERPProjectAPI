using MediatR;
using Microsoft.Extensions.Logging;

public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only the name: requests carry passwords, staff codes, licence keys and tokens, which must never reach a log.
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);

        var response = await next();

        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);

        return response;
    }
}
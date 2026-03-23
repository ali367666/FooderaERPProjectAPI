using System.Net;
using System.Text.Json;
using FluentValidation;

namespace API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .Select(x => new
                {
                    Property = x.PropertyName,
                    Error = x.ErrorMessage,
                    AttemptedValue = x.AttemptedValue
                })
                .ToList();

            _logger.LogWarning(
                "Validation xətası baş verdi. Path: {Path}, Method: {Method}, Errors: {@Errors}",
                context.Request.Path,
                context.Request.Method,
                errors);

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors.Select(x => x.Error).ToList()
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Gözlənilməz xəta baş verdi. Path: {Path}, Method: {Method}",
                context.Request.Path,
                context.Request.Method);

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Success = false,
                Message = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
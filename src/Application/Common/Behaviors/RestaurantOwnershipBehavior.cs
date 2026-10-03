using System.Collections.Concurrent;
using System.Reflection;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>
/// Tenant guard for branch and table references: any request carrying a <c>RestaurantId</c> or
/// <c>TableId</c> — on the command/query itself or on its <c>Request</c> DTO — must point at a
/// branch/table of the caller's own company. Stops a user from creating or reading printers,
/// tables, cash movements, shifts, reservations, etc. against another company's branch by sending
/// its id. The SuperAdmin and anonymous endpoints (public QR menu, login) are not restricted here.
/// </summary>
public class RestaurantOwnershipBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<(Type, string), Func<object, int?>?> Readers = new();

    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly ICurrentUserService _currentUserService;

    public RestaurantOwnershipBehavior(
        IRestaurantRepository restaurantRepository,
        IRestaurantTableRepository tableRepository,
        ICurrentUserService currentUserService)
    {
        _restaurantRepository = restaurantRepository;
        _tableRepository = tableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_currentUserService.IsSuperAdmin || _currentUserService.CompanyId <= 0 || IsPublicEndpoint(typeof(TRequest)))
            return await next();

        var companyId = _currentUserService.CompanyId;

        var restaurantId = Read(request, "RestaurantId");
        if (restaurantId is > 0)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId.Value, cancellationToken);
            if (restaurant is null || restaurant.CompanyId != companyId)
                throw new NotFoundException("Filial tapılmadı.");
        }

        var tableId = Read(request, "TableId");
        if (tableId is > 0 && await _tableRepository.GetByIdAsync(tableId.Value, companyId, cancellationToken) is null)
            throw new NotFoundException("Masa tapılmadı.");

        return await next();
    }

    /// <summary>
    /// Anonymous by design — the public QR menu, login (incl. POS login) and the login-screen
    /// branding. A browser may still send a leftover token from another company, which must not
    /// block them.
    /// </summary>
    private static bool IsPublicEndpoint(Type requestType)
    {
        var ns = requestType.Namespace ?? "";
        return ns.StartsWith("Application.PublicMenu", StringComparison.Ordinal)
            || ns.StartsWith("Application.Auth", StringComparison.Ordinal)
            || requestType.Name == "GetCompanySettingsBrandingQuery";
    }

    private static int? Read(TRequest request, string propertyName) =>
        Readers.GetOrAdd((typeof(TRequest), propertyName), key => BuildReader(key.Item1, key.Item2))?.Invoke(request);

    /// <summary>Finds the id property on the request or on its Request DTO; null when neither has one.</summary>
    private static Func<object, int?>? BuildReader(Type requestType, string propertyName)
    {
        var direct = FindIntProperty(requestType, propertyName);
        if (direct is not null)
            return r => direct.GetValue(r) as int?;

        var dtoProp = requestType.GetProperty("Request", BindingFlags.Public | BindingFlags.Instance);
        var nested = dtoProp is null ? null : FindIntProperty(dtoProp.PropertyType, propertyName);
        if (dtoProp is not null && nested is not null)
            return r => dtoProp.GetValue(r) is { } dto ? nested.GetValue(dto) as int? : null;

        return null;
    }

    private static PropertyInfo? FindIntProperty(Type type, string name)
    {
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        return prop is not null && (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?)) ? prop : null;
    }
}

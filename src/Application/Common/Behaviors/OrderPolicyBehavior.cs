using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItems.Commands.Create;
using Application.MenuItems.Commands.Update;
using Application.OrderLines.Commands.Add;
using Application.OrderLines.Commands.Delete;
using Application.Orders.Commands.Cancel;
using Application.Orders.Commands.Delete;
using Application.Orders.Commands.Pay;
using Domain.Constants;
using Domain.Enums;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>
/// Company-settings rules for POS sales that apply across several order commands, kept in one
/// place instead of being repeated in each handler:
/// - "Ofisiant ləğv edə bilsin" (WaiterCanCancel) — a Waiter-role user can't remove lines or cancel orders.
/// - "Məhsul kodu zorunlu" (RequireProductCode) — items without a product code can't be sold or saved.
/// - "Ödəniş siyahısı" — a payment method switched off in settings is rejected.
/// </summary>
public class OrderPolicyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public OrderPolicyBehavior(
        ICompanySettingsRepository companySettingsRepository,
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _companySettingsRepository = companySettingsRepository;
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var isCancel = request is DeleteOrderLineCommand or DeleteOrderCommand or CancelOrderCommand;
        var isCodeCheck = request is AddOrderLineCommand or CreateMenuItemCommand or UpdateMenuItemCommand;
        var isPay = request is PayOrderCommand;
        if (!isCancel && !isCodeCheck && !isPay)
            return await next();

        var companyId = _currentUserService.CompanyId;
        var settings = await _companySettingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        if (settings is null)
            return await next();

        if (isCancel && !settings.WaiterCanCancel && _currentUserService.IsInRole(AppRoles.Waiter))
            throw new BadRequestException("Ofisiant məhsulu silə və ya sifarişi ləğv edə bilməz — menecerə müraciət edin.");

        if (request is PayOrderCommand pay
            && Enum.TryParse<PaymentMethod>(pay.Request.PaymentMethod, true, out var method)
            && !IsPaymentMethodEnabled(settings, method))
            throw new BadRequestException("Bu ödəniş üsulu tənzimləmələrdə deaktiv edilib.");

        if (settings.RequireProductCode)
        {
            switch (request)
            {
                case AddOrderLineCommand add:
                    var menuItem = await _menuItemRepository.GetByIdAsync(add.Request.MenuItemId, companyId, cancellationToken);
                    if (menuItem is not null && !HasProductCode(menuItem.Barcode, menuItem.UnitId, menuItem.WeightCode))
                        throw new BadRequestException($"'{menuItem.Name}' məhsulunun kodu yoxdur — kodsuz məhsul satıla bilməz.");
                    break;
                case CreateMenuItemCommand create when !HasProductCode(create.Request.Barcode, create.Request.UnitId, "auto"):
                case UpdateMenuItemCommand update when !HasProductCode(update.Request.Barcode, update.Request.UnitId, "auto"):
                    throw new BadRequestException("Məhsul kodu (barkod) məcburidir.");
            }
        }

        return await next();
    }

    /// <summary>
    /// Every item gets an automatic weight code, so the "product code" is its barcode — except for
    /// items sold by weight, where the (scale) weight code is the code.
    /// </summary>
    private static bool HasProductCode(string? barcode, int unitId, string? weightCode) =>
        !string.IsNullOrWhiteSpace(barcode)
        || (OrderLinePricing.IsWeightBased(unitId) && !string.IsNullOrWhiteSpace(weightCode));

    private static bool IsPaymentMethodEnabled(Domain.Entities.CompanySettings settings, PaymentMethod method) =>
        method switch
        {
            PaymentMethod.Cash => settings.PaymentCashEnabled,
            PaymentMethod.Card => settings.PaymentCardEnabled,
            PaymentMethod.Credit => settings.PaymentCreditEnabled,
            _ => true
        };
}

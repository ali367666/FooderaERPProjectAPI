using Application.Common.Exceptions;

namespace Application.Common.Helpers;

public static class OrderGuards
{
    /// <summary>
    /// "Hesab verildikdən sonra hesaba müdaxilə edilə bilməz" — once the bill has been printed with
    /// LockOrderAfterBill on, the order's contents are frozen until a manager unlocks it.
    /// </summary>
    public static void EnsureNotBillLocked(Domain.Entities.Order order)
    {
        if (order.IsBillLocked)
            throw new BadRequestException(
                "Hesab artıq verilib — sifarişə dəyişiklik etmək olmaz. Kilidi açmaq üçün menecerə müraciət edin.");
    }
}

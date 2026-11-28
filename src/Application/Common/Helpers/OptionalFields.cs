using Application.Common.Exceptions;

namespace Application.Common.Helpers;

/// <summary>
/// Fields added to existing update requests. A screen that does not send them must not wipe them: null keeps the saved value,
/// an empty text clears it.
/// </summary>
public static class OptionalFields
{
    public static string? Text(string? sent, string? saved, int maxLength, string name)
    {
        if (sent is null)
            return saved;
        var value = sent.Trim();
        if (value.Length > maxLength)
            throw new BadRequestException($"{name} {maxLength} simvoldan çox ola bilməz.");
        return value.Length == 0 ? null : value;
    }

    public static decimal MinimumStock(decimal? sent, decimal saved)
    {
        if (sent < 0)
            throw new BadRequestException("Minimum stok mənfi ola bilməz.");
        return sent ?? saved;
    }
}

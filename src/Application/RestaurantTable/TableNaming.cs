using System.Globalization;

namespace Application.RestaurantTables;

/// <summary>Orders names the way people count them: "Masa 2" before "Masa 10".</summary>
public sealed class NaturalNameComparer : IComparer<string>
{
    public static readonly NaturalNameComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;
        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
            {
                var a = ReadDigits(x, ref i).TrimStart('0');
                var b = ReadDigits(y, ref j).TrimStart('0');
                var byNumber = a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
                if (byNumber != 0)
                    return byNumber;
                continue;
            }

            var byChar = string.Compare(x, i, y, j, 1, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            if (byChar != 0)
                return byChar;
            i++;
            j++;
        }

        return (x.Length - i).CompareTo(y.Length - j);
    }

    private static string ReadDigits(string value, ref int index)
    {
        var start = index;
        while (index < value.Length && char.IsAsciiDigit(value[index]))
            index++;
        return value[start..index];
    }
}

/// <summary>Names and floor-plan places for tables created many at once.</summary>
public static class TableBulkPlanner
{
    public const int MaxCount = 200;
    public const int TablesPerRow = 10;
    public const int TableSize = 80;
    private const int Gap = 20;

    /// <summary>
    /// "{prefix} {number}" (just the number without a prefix). Numbering continues after the highest number already used with that prefix,
    /// unless a start is given; names that are taken are skipped, so exactly <paramref name="count"/> new names come back.
    /// </summary>
    public static List<string> Names(IEnumerable<string> existingNames, string? prefix, int count, int? startNumber = null)
    {
        var head = string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix.Trim() + " ";
        var taken = new HashSet<string>(existingNames.Select(n => n.Trim()), StringComparer.OrdinalIgnoreCase);

        var number = startNumber ?? taken
            .Where(n => n.StartsWith(head, StringComparison.OrdinalIgnoreCase))
            .Select(n => int.TryParse(n.AsSpan(head.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var used) ? used : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var names = new List<string>(count);
        for (; names.Count < count; number++)
        {
            var name = head + number.ToString(CultureInfo.InvariantCulture);
            if (!taken.Contains(name))
                names.Add(name);
        }

        return names;
    }

    /// <summary>The place of the n-th new table: rows of ten, starting at <paramref name="top"/>, so nothing lands on top of anything.</summary>
    public static (int X, int Y) Position(int index, int top)
        => (Gap + index % TablesPerRow * (TableSize + Gap), top + index / TablesPerRow * (TableSize + Gap));

    /// <summary>The first free row under the tables already on the floor plan.</summary>
    public static int TopBelow(IEnumerable<(int Y, int Height)> existing)
        => existing.Select(t => t.Y + t.Height).DefaultIfEmpty(0).Max() + Gap;
}

namespace Application.Banquets.Services;

public interface IBanquetTableChecker
{
    /// <summary>
    /// The chosen tables, in the order given. Throws unless every one exists, belongs to the branch,
    /// is active and is a real dine-in table, and at least one is picked.
    /// </summary>
    Task<List<Domain.Entities.RestaurantTable>> LoadTablesAsync(
        int companyId, int restaurantId, IReadOnlyCollection<int> tableIds, CancellationToken cancellationToken);

    /// <summary>
    /// Every active dine-in table of a banquet hall. Throws unless the hall exists in the branch, is active,
    /// is marked as a banquet hall and has at least one table.
    /// </summary>
    Task<List<Domain.Entities.RestaurantTable>> LoadHallTablesAsync(
        int companyId, int restaurantId, int sectionId, CancellationToken cancellationToken);

    /// <summary>The reason the table can't take a banquet right now (an open order on it), or null.</summary>
    Task<string?> GetBusyReasonNowAsync(int companyId, Domain.Entities.RestaurantTable table, CancellationToken cancellationToken);

    /// <summary>The reason the table is already booked at that time (another reservation overlaps), or null.</summary>
    Task<string?> GetBookedReasonAsync(
        int companyId, Domain.Entities.RestaurantTable table, DateTime start, int durationMinutes, CancellationToken cancellationToken);
}

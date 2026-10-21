namespace Application.Banquets.Services;

public interface IBanquetTableChecker
{
    /// <summary>
    /// The chosen tables, in the order given. Throws unless every one exists, belongs to the branch,
    /// is active and is a real dine-in table, and at least one is picked.
    /// </summary>
    Task<List<Domain.Entities.RestaurantTable>> LoadTablesAsync(
        int companyId, int restaurantId, IReadOnlyCollection<int> tableIds, CancellationToken cancellationToken);

    /// <summary>The reason the table can't take a banquet right now (open order, reservation inside the block window), or null.</summary>
    Task<string?> GetBusyReasonNowAsync(int companyId, Domain.Entities.RestaurantTable table, CancellationToken cancellationToken);

    /// <summary>The reason the table is already booked at that time (another reservation overlaps), or null.</summary>
    Task<string?> GetBookedReasonAsync(
        int companyId, Domain.Entities.RestaurantTable table, DateTime start, int durationMinutes, CancellationToken cancellationToken);
}

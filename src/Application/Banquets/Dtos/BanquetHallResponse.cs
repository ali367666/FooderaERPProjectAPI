namespace Application.Banquets.Dtos;

/// <summary>A banquet hall as the reservation screen offers it: free for the asked time, or busy with the reason why not.</summary>
public class BanquetHallResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int TableCount { get; set; }

    /// <summary>Total seats of the hall's tables.</summary>
    public int Capacity { get; set; }

    /// <summary>False when any table of the hall is busy (or booked at the asked time) - a hall is taken as a whole.</summary>
    public bool IsAvailable { get; set; }

    /// <summary>Why the hall can't be picked (first busy table and its reason); null when it is free.</summary>
    public string? BusyReason { get; set; }
}

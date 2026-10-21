namespace Application.Banquets.Dtos;

/// <summary>A table of a hall as the banquet screen lists it: free to pick, or busy with the reason why not.</summary>
public class BanquetTableResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public int? SectionId { get; set; }
    public string? SectionName { get; set; }
    public bool IsAvailable { get; set; }

    /// <summary>Why the table can't be picked ("Dolu: açıq sifariş var", a reservation...); null when it is free.</summary>
    public string? BusyReason { get; set; }
}

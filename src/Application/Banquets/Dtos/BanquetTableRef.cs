namespace Application.Banquets.Dtos;

/// <summary>A table that belongs to a banquet order or booking.</summary>
public class BanquetTableRef
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public int? SectionId { get; set; }
}

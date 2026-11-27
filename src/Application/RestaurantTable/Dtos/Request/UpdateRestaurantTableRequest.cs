namespace Application.RestaurantTable.Dtos.Request;

public class UpdateRestaurantTableRequest
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Hall (RestaurantSection) of the table. Omitted = unchanged, 0 = take it out of its hall, otherwise the hall id.</summary>
    public int? SectionId { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Note { get; set; }
    public Domain.Enums.RestaurantTableType Type { get; set; } = Domain.Enums.RestaurantTableType.Masa;
}
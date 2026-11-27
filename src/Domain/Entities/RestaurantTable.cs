using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class RestaurantTable : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public RestaurantTableType Type { get; set; } = RestaurantTableType.Masa;

    public bool IsActive { get; set; } = true;
    public bool IsOccupied { get; set; } = false;

    public int? SectionId { get; set; }
    public RestaurantSection? Section { get; set; }

    /// <summary>
    /// Unguessable id printed in this table's own QR code (<c>/menu/{restaurantId}?t={token}</c>), so a scan
    /// - and any order placed from it later - is tied to this table. Created on first use.
    /// </summary>
    public string? QrToken { get; set; }

    public decimal? HourlyRate { get; set; }
    public string? Note { get; set; }

    // Floor plan position
    public int PosX { get; set; } = 0;
    public int PosY { get; set; } = 0;
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 80;
    public string Shape { get; set; } = "square"; // square, round, rectangle
    public int Rotation { get; set; } = 0;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// The menu the QR codes of a branch show, frozen when someone presses "update QR". Products added or
/// re-priced afterwards stay off the QR menu until the next publish.
/// </summary>
public class PublicMenuSnapshot : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    /// <summary>The published categories and products, serialized.</summary>
    public string MenuJson { get; set; } = default!;

    /// <summary>Fingerprint of <see cref="MenuJson"/>; compared with the live menu to tell whether an update is pending.</summary>
    public string ContentHash { get; set; } = default!;

    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
}

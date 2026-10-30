namespace Application.QrMenu.Dtos;

/// <summary>What the QR screen of a branch needs: when the menu was last published, whether it is stale, and each table's QR token.</summary>
public class QrMenuStatusResponse
{
    public int RestaurantId { get; set; }

    /// <summary>When the QR menu was last published; null if it never was (the live menu is shown meanwhile).</summary>
    public DateTime? PublishedAtUtc { get; set; }

    /// <summary>True when a product, price, picture or category changed since the last publish - time to press "update QR".</summary>
    public bool HasPendingChanges { get; set; }

    public List<QrMenuTableResponse> Tables { get; set; } = new();
}

public class QrMenuTableResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int? SectionId { get; set; }
    public string? SectionName { get; set; }

    /// <summary>Goes into this table's QR link as <c>?t={qrToken}</c>.</summary>
    public string QrToken { get; set; } = default!;
}

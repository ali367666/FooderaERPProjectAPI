using Domain.Common;

namespace Domain.Entities;

public class CompanySettings : CompanyEntity<int>
{
    // Ümumi
    public TimeSpan? OpeningTime { get; set; }

    public bool ModuleFilial { get; set; } = true;
    public bool ModuleAnbar { get; set; } = true;
    public bool ModuleRezervasyon { get; set; } = true;
    public bool ModuleMasaBolge { get; set; } = true;

    public bool IntegrationWolt { get; set; }
    public bool IntegrationBolt { get; set; }
    public bool Integration189Delivery { get; set; }

    public int? AlertMilliseconds { get; set; }
    public int? AlertRingCount { get; set; }
    public int? AlertRingIntervalSeconds { get; set; }

    // Qəbz
    public string? LoginLogoUrl { get; set; }
    public string? ReportLogoUrl { get; set; }
    public string? WallpaperUrl { get; set; }
    public string? LoginLocation { get; set; }
    public int? TransparencyLevel { get; set; }
    public string? ProductColor { get; set; }
    public string? FloorLabel { get; set; }
    public string? Slogan { get; set; }
    public string? SocialLinks { get; set; }
    public string? ContactPhoneNumber { get; set; }
    public int? ReceiptFontSize { get; set; }
    public int? CategoryFontSize { get; set; }
    public bool AllowReceiptEditAfterPrint { get; set; } = true;
    public bool WaiterCanPrintCustomerReceipt { get; set; } = true;
}

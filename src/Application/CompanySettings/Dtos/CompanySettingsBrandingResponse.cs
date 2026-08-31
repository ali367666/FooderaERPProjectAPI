namespace Application.CompanySettings.Dtos;

public class CompanySettingsBrandingResponse
{
    public string? LoginLogoUrl { get; set; }
    public string? ReportLogoUrl { get; set; }
    public string? WallpaperUrl { get; set; }
    public string? LoginLocation { get; set; }
    public int? TransparencyLevel { get; set; }
    public string? FloorLabel { get; set; }
    public string? SocialLinks { get; set; }
    public string? Slogan { get; set; }
    public string? ProductColor { get; set; }
    public string? ContactPhoneNumber { get; set; }
    public int? ReceiptFontSize { get; set; }
    public int? CategoryFontSize { get; set; }
    public bool AllowReceiptEditAfterPrint { get; set; } = true;
    public bool WaiterCanPrintCustomerReceipt { get; set; } = true;

    public int? AlertMilliseconds { get; set; }
    public int? AlertRingCount { get; set; }
    public int? AlertRingIntervalSeconds { get; set; }
    public int? TableTimeWarningMinutes { get; set; }

    public bool ModuleFilial { get; set; }
    public bool ModuleAnbar { get; set; }
    public bool ModuleRezervasyon { get; set; }
    public bool ModuleMasaBolge { get; set; }
}

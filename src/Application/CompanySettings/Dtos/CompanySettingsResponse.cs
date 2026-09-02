namespace Application.CompanySettings.Dtos;

public class CompanySettingsResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    public TimeSpan? OpeningTime { get; set; }

    public bool ModuleFilial { get; set; }
    public bool ModuleAnbar { get; set; }
    public bool ModuleRezervasyon { get; set; }
    public bool ModuleMasaBolge { get; set; }
    public bool ModulePaket { get; set; }
    public bool ModuleOtel { get; set; }
    public bool ModuleFitnes { get; set; }
    public bool ModuleDataSecimi { get; set; }
    public bool ModuleQiymetSor { get; set; }

    public bool IntegrationWolt { get; set; }
    public bool IntegrationBolt { get; set; }
    public bool Integration189Delivery { get; set; }

    public int? AlertMilliseconds { get; set; }
    public int? AlertRingCount { get; set; }
    public int? AlertRingIntervalSeconds { get; set; }
    public int? TableTimeWarningMinutes { get; set; }

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
    public int? ReceiptRestaurantNameFontSize { get; set; }
    public bool AllowReceiptEditAfterPrint { get; set; }
    public bool WaiterCanPrintCustomerReceipt { get; set; }

    public bool PrintAutoOnPayment { get; set; }
    public bool PrintKitchenOnPayment { get; set; }
    public bool PrintShowPreview { get; set; }
    public bool PrintGroupQuantities { get; set; }
    public bool ReceiptShowTime { get; set; }
    public bool ReceiptShowWaiterName { get; set; }
    public bool ReceiptShowTableName { get; set; }
    public bool ReceiptShowOrderNumber { get; set; }
    public bool ReceiptShowPaymentMethod { get; set; }

    public bool AskGuestCountOnOpen { get; set; }

    public decimal? DefaultVatPercent { get; set; }
}

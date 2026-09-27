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
    public bool ModulePaket { get; set; }
    public bool ModuleOtel { get; set; }
    public bool ModuleFitnes { get; set; }
    public bool ModuleDataSecimi { get; set; }
    public bool ModuleQiymetSor { get; set; }

    /// <summary>
    /// İstirahət kompleksi rejimi — aktiv olanda bu şirkətin hər Filialı öz ayrıca modul dəstini
    /// (RestaurantSettings) təyin edə bilir, əks halda hamısı yuxarıdakı Company-səviyyəli
    /// modullardan istifadə edir.
    /// </summary>
    public bool ModuleKompleks { get; set; }

    public bool IntegrationWolt { get; set; }
    public bool IntegrationBolt { get; set; }
    public bool Integration189Delivery { get; set; }

    public int? AlertMilliseconds { get; set; }
    public int? AlertRingCount { get; set; }
    public int? AlertRingIntervalSeconds { get; set; }
    public int? TableTimeWarningMinutes { get; set; }

    // Qəbz
    public string? LoginLogoUrl { get; set; }
    public string? ReportLogoUrl { get; set; }
    public string? WallpaperUrl { get; set; }
    public string? LoginLocation { get; set; }
    public int? TransparencyLevel { get; set; }
    public string? ProductColor { get; set; }
    public string? FloorLabel { get; set; }
    public int? LogoSize { get; set; }
    public string? Slogan { get; set; }
    public string? SocialLinks { get; set; }
    public string? ContactPhoneNumber { get; set; }
    public int? ReceiptFontSize { get; set; }
    public int? CategoryFontSize { get; set; }
    public int? ReceiptRestaurantNameFontSize { get; set; }
    public bool AllowReceiptEditAfterPrint { get; set; } = true;
    public bool WaiterCanPrintCustomerReceipt { get; set; } = true;

    // Çap paneli
    public bool PrintAutoOnPayment { get; set; }
    public bool PrintKitchenOnPayment { get; set; }
    public bool PrintShowPreview { get; set; } = true;
    public bool PrintGroupQuantities { get; set; } = true;
    public bool PrintKitchenGroupQuantities { get; set; }
    public bool ReceiptShowTime { get; set; } = true;
    public bool ReceiptShowWaiterName { get; set; } = true;
    public bool ReceiptShowTableName { get; set; } = true;
    public bool ReceiptShowOrderNumber { get; set; } = true;
    public bool ReceiptShowPaymentMethod { get; set; } = true;

    /// <summary>When true, an auto-triggered print (on payment, or when preview is off) asks for
    /// confirmation first instead of printing immediately.</summary>
    public bool PrintAskBeforeAutoPrint { get; set; }

    /// <summary>When true, the receipt uses the ReceiptSimpleShow* flags below instead of the
    /// normal ReceiptShow* flags — each company decides independently what appears in each mode.</summary>
    public bool ReceiptSimpleMode { get; set; }

    public bool ReceiptSimpleShowOrderNumber { get; set; }
    public bool ReceiptSimpleShowWaiterName { get; set; }
    public bool ReceiptSimpleShowTime { get; set; }
    public bool ReceiptSimpleShowPaymentMethod { get; set; }
    public bool ReceiptSimpleShowVat { get; set; }
    public bool ReceiptSimpleShowFooter { get; set; }

    /// <summary>Print the business (branch) name at the top of kitchen tickets.</summary>
    public bool PrintKitchenShowBusinessName { get; set; } = true;

    /// <summary>Print the business (branch) name at the top of the customer bill (adisyon).</summary>
    public bool ReceiptShowBusinessName { get; set; } = true;

    /// <summary>When true, kitchen tickets may be sent while the order is on hold — the ticket is
    /// marked "GÖZLƏMƏDƏ" so the kitchen knows not to start yet. When false, held orders are blocked.</summary>
    public bool PrintKitchenOnHold { get; set; }

    /// <summary>When a table is moved, automatically print a transfer document to every kitchen
    /// printer that already received lines of that order.</summary>
    public bool PrintTransferDocAuto { get; set; }

    /// <summary>Print the table-transfer document in two copies.</summary>
    public bool PrintTransferDocDouble { get; set; }

    /// <summary>ChiefPrint — every kitchen ticket is also copied to the branch's chief printer
    /// (Printer.IsChiefPrinter).</summary>
    public bool PrintChiefCopy { get; set; }

    /// <summary>Satış ekranında "Marş" düyməsi — mətbəxə marş çeki göndərir və gözləmələri açır.</summary>
    public bool PosMarsEnabled { get; set; }

    /// <summary>Barkodu/çəki kodu olmayan məhsul satıla bilməz, menyu formasında kod məcburidir.</summary>
    public bool RequireProductCode { get; set; }

    /// <summary>Ödəniş siyahısı — POS-da Nağd ödəniş üsulu görünsün.</summary>
    public bool PaymentCashEnabled { get; set; } = true;

    /// <summary>Ödəniş siyahısı — POS-da Kart ödəniş üsulu görünsün.</summary>
    public bool PaymentCardEnabled { get; set; } = true;

    /// <summary>Ödəniş siyahısı — POS-da Borca yaz üsulu görünsün.</summary>
    public bool PaymentCreditEnabled { get; set; } = true;

    /// <summary>Mətbəxə göndərəndə sifarişin ofisiantı öz kodu ilə təsdiqləməlidir.</summary>
    public bool WaiterConfirmWithPin { get; set; }

    /// <summary>Waiter rolu məhsulu silə / sifarişi ləğv edə bilsin.</summary>
    public bool WaiterCanCancel { get; set; } = true;


    /// <summary>Hesab (qəbz) verildikdən sonra sifarişə müdaxilə edilə bilməz.</summary>
    public bool LockOrderAfterBill { get; set; }

    /// <summary>Mətbəxdə hər məhsul ayrıca qəbzdə çıxsın.</summary>
    public bool PrintKitchenSeparateTickets { get; set; }

    /// <summary>POS-1 — ödənişdə pul qalığı paneli (alınan məbləğ / qaytarılan).</summary>
    public bool PosShowChangePanel { get; set; } = true;

    /// <summary>POS-1 — çəkili məhsul seçiləndə tartım pəncərəsi açılsın.</summary>
    public bool PosShowWeighWindow { get; set; } = true;

    /// <summary>POS-1 — satışda müştəri seçmə düyməsi.</summary>
    public bool PosShowCustomerSelect { get; set; } = true;

    /// <summary>POS-1 — gözlətmə düymələri (sifariş və məhsul).</summary>
    public bool PosShowHoldButton { get; set; } = true;

    /// <summary>POS-1 — satış ekranında saat.</summary>
    public bool PosShowClock { get; set; } = true;

    /// <summary>Masa paneli — masada ofisiantın adı.</summary>
    public bool TableShowWaiter { get; set; } = true;

    /// <summary>Masa paneli — masa/sifariş qeydi.</summary>
    public bool TableShowNote { get; set; } = true;

    /// <summary>Masa paneli — masanın açıq qaldığı vaxt.</summary>
    public bool TableShowTime { get; set; } = true;

    /// <summary>Masa paneli — masadakı məbləğ.</summary>
    public bool TableShowAmount { get; set; } = true;

    /// <summary>Masa paneli — vaxt keçəndə məşğul masa xəbərdarlığı (qırmızı çərçivə + səs).</summary>
    public bool TableBusyWarning { get; set; } = true;

    /// <summary>Masa paneli — rezerv olunmuş masanı açarkən xəbərdarlıq.</summary>
    public bool TableReservationWarning { get; set; } = true;

    /// <summary>Masa və paket qiymətləri dəzgah qiymətindən götürülsün.</summary>
    public bool TablePricesFromStation { get; set; } = true;

    /// <summary>Neytral — dəzgah qiyməti yoxdursa anbar satış qiymətindən istifadə olunsun.</summary>
    public bool PriceFromWarehouseSale { get; set; } = true;

    // Tema
    /// <summary>Main brand colour (hex, e.g. #2563eb) used for buttons and highlights in the panel and POS.</summary>
    public string? ThemePrimaryColor { get; set; }
    /// <summary>Corner style: "square", "medium" (default) or "round".</summary>
    public string? ThemeRadius { get; set; }

    // Çek dizaynı
    /// <summary>Receipt paper width in mm — 58 (32 characters per line) or 80 (48).</summary>
    public int ReceiptPaperWidth { get; set; } = 80;
    /// <summary>Free text printed at the top of the customer receipt.</summary>
    public string? ReceiptHeaderText { get; set; }
    /// <summary>Free text printed at the bottom of the customer receipt.</summary>
    public string? ReceiptFooterText { get; set; }
    /// <summary>Line order on the customer receipt: "order" (as ordered), "name" (A–Z) or "category" (grouped by category).</summary>
    public string ReceiptSortMode { get; set; } = "order";

    /// <summary>Toxunuşlu ekran — larger POS controls and on-screen number pads, for touch terminals without a keyboard.</summary>
    public bool TouchScreenMode { get; set; }

    // POS-1 paneli
    public bool AskGuestCountOnOpen { get; set; }

    /// <summary>When true, a table/order can only be opened or edited by the waiter who owns it —
    /// even a user with the "view all tables" permission is blocked, no manager override.</summary>
    public bool SingleWaiterMode { get; set; }

    // ƏDV
    public decimal? DefaultVatPercent { get; set; }
}

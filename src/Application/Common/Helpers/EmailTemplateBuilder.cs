namespace Infrastructure.Services;

public static class EmailTemplateBuilder
{
    public static string BuildBasicTemplate(string title, string message)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; background-color: #f8f9fa; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e5e5e5; border-radius: 10px; padding: 24px;'>
                <h2 style='margin-top: 0; color: #222;'>{title}</h2>
                <p style='color: #444; font-size: 15px;'>{message}</p>
            </div>
        </body>
        </html>";
    }

    public static string BuildStockRequestTemplate(
        int stockRequestId,
        string requestingWarehouse,
        string supplyingWarehouse,
        string? note)
    {
        var safeNote = string.IsNullOrWhiteSpace(note) ? "-" : note;

        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; background-color: #f8f9fa; padding: 20px;'>
            <div style='max-width: 650px; margin: 0 auto; background: #ffffff; border: 1px solid #e5e5e5; border-radius: 10px; padding: 24px;'>
                <h2 style='margin-top: 0; color: #222;'>Yeni stok sorğusu</h2>
                <p style='color: #444;'>Sizin anbar üçün yeni stok sorğusu yaradıldı.</p>

                <table style='width: 100%; border-collapse: collapse; margin-top: 16px;'>
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Request No</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>#{stockRequestId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Requesting Warehouse</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{requestingWarehouse}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Supplying Warehouse</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{supplyingWarehouse}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Note</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{safeNote}</td>
                    </tr>
                </table>
            </div>
        </body>
        </html>";
    }
}
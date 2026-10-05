namespace Application.Common.Interfaces.Abstracts.İnterfaces;

public interface INetworkPrinterService
{
    Task PrintAsync(string ipAddress, int port, string content, CancellationToken cancellationToken);

    /// <summary>
    /// Prints a 1-bit image (ESC/POS raster, GS v 0). Used for the customer receipt: it is drawn as
    /// a picture so the logo and every Azerbaijani letter (ə, ğ, ı, İ, ö, ş, ü, ç) print correctly —
    /// no ESC/POS text code page contains "ə". <paramref name="trailer"/> (e.g. a barcode command)
    /// is sent as-is after the image.
    /// </summary>
    Task PrintRasterAsync(string ipAddress, int port, int widthDots, int height, byte[] bits, string? trailer,
        CancellationToken cancellationToken);
}

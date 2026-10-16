namespace Application.Printer.Dtos;

public class PrintImageRequest
{
    public int WidthDots { get; set; }
    public int Height { get; set; }
    /// <summary>Base64 of the packed 1-bit rows (MSB first, 1 = black).</summary>
    public string Data { get; set; } = default!;
    /// <summary>Optional ESC/POS commands sent after the image (e.g. the receipt barcode).</summary>
    public string? Trailer { get; set; }
}

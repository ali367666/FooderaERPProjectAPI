using System.Net.Sockets;
using System.Text;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class NetworkPrinterService : INetworkPrinterService
{
    private const int ConnectTimeoutMs = 5000;

    private readonly ILogger<NetworkPrinterService> _logger;

    public NetworkPrinterService(ILogger<NetworkPrinterService> logger)
    {
        _logger = logger;
    }

    public async Task PrintAsync(string ipAddress, int port, string content, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();

        try
        {
            var connectTask = client.ConnectAsync(ipAddress, port, cancellationToken).AsTask();
            var completed = await Task.WhenAny(connectTask, Task.Delay(ConnectTimeoutMs, cancellationToken));
            if (completed != connectTask || !client.Connected)
            {
                throw new Exception($"Printerə qoşulmaq mümkün olmadı ({ipAddress}:{port}).");
            }

            await using var stream = client.GetStream();
            var bytes = Encoding.UTF8.GetBytes(content + "\n\n\n");
            await stream.WriteAsync(bytes, cancellationToken);

            // ESC/POS: partial cut
            var cutCommand = new byte[] { 0x1D, 0x56, 0x42, 0x00 };
            await stream.WriteAsync(cutCommand, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Printerə çap göndərilmədi. IP: {IpAddress}, Port: {Port}", ipAddress, port);
            throw new Exception($"Printerə qoşulmaq mümkün olmadı ({ipAddress}:{port}).", ex);
        }
    }

    // Many printers buffer a limited image height per GS v 0 command, so tall receipts go in bands.
    private const int RasterBandHeight = 256;

    public async Task PrintRasterAsync(string ipAddress, int port, int widthDots, int height, byte[] bits, string? trailer,
        CancellationToken cancellationToken)
    {
        using var client = new TcpClient();

        try
        {
            var connectTask = client.ConnectAsync(ipAddress, port, cancellationToken).AsTask();
            var completed = await Task.WhenAny(connectTask, Task.Delay(ConnectTimeoutMs, cancellationToken));
            if (completed != connectTask || !client.Connected)
            {
                throw new Exception($"Printerə qoşulmaq mümkün olmadı ({ipAddress}:{port}).");
            }

            await using var stream = client.GetStream();
            await stream.WriteAsync(new byte[] { 0x1B, 0x40 }, cancellationToken); // ESC @ — reset

            var bytesPerRow = widthDots / 8;
            for (var top = 0; top < height; top += RasterBandHeight)
            {
                var rows = Math.Min(RasterBandHeight, height - top);
                // GS v 0 m xL xH yL yH — m = 0 (normal size), x in bytes, y in dots
                var header = new byte[]
                {
                    0x1D, 0x76, 0x30, 0x00,
                    (byte)(bytesPerRow & 0xFF), (byte)(bytesPerRow >> 8),
                    (byte)(rows & 0xFF), (byte)(rows >> 8),
                };
                await stream.WriteAsync(header, cancellationToken);
                await stream.WriteAsync(bits.AsMemory(top * bytesPerRow, rows * bytesPerRow), cancellationToken);
            }

            if (!string.IsNullOrEmpty(trailer))
                await stream.WriteAsync(Encoding.UTF8.GetBytes(trailer), cancellationToken);

            // Feed past the cutter, then ESC/POS partial cut.
            await stream.WriteAsync(Encoding.ASCII.GetBytes("\n\n\n"), cancellationToken);
            await stream.WriteAsync(new byte[] { 0x1D, 0x56, 0x42, 0x00 }, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Çek şəkli printerə göndərilmədi. IP: {IpAddress}, Port: {Port}", ipAddress, port);
            throw new Exception($"Printerə qoşulmaq mümkün olmadı ({ipAddress}:{port}).", ex);
        }
    }
}

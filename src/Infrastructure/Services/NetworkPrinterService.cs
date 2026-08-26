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
}

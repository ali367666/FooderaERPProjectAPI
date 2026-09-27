using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PrinterDiscoveryService : IPrinterDiscoveryService
{
    private const int RawPrintPort = 9100;
    private const int ProbeTimeoutMs = 400;
    private const int MaxParallelProbes = 64;

    private readonly ILogger<PrinterDiscoveryService> _logger;

    public PrinterDiscoveryService(ILogger<PrinterDiscoveryService> logger)
    {
        _logger = logger;
    }

    public async Task<List<DiscoveredPrinter>> DiscoverAsync(CancellationToken cancellationToken)
    {
        var installedTask = GetInstalledPrintersAsync(cancellationToken);
        var networkTask = ScanLocalNetworkAsync(cancellationToken);
        await Task.WhenAll(installedTask, networkTask);

        var installed = installedTask.Result;
        var knownIps = installed.Where(x => x.IpAddress != null).Select(x => x.IpAddress!).ToHashSet();

        // A network printer that is also installed on this computer is listed once, under its installed name.
        return installed
            .Concat(networkTask.Result.Where(x => !knownIps.Contains(x.IpAddress!)))
            .ToList();
    }

    /// <summary>Windows printers with their TCP/IP port address, read through PowerShell's PrintManagement module.</summary>
    private async Task<List<DiscoveredPrinter>> GetInstalledPrintersAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            return new List<DiscoveredPrinter>();

        const string script =
            "$ports = @{}; Get-PrinterPort | ForEach-Object { $ports[$_.Name] = $_ }; " +
            "Get-Printer | ForEach-Object { $p = $ports[$_.PortName]; " +
            "[pscustomobject]@{ Name = $_.Name; Host = $p.PrinterHostAddress; Port = $p.PortNumber } } | " +
            "ConvertTo-Json -Compress";

        using var process = new Process();
        try
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                ArgumentList = { "-NoProfile", "-NonInteractive", "-Command", script },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var output = await process.StandardOutput.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            if (string.IsNullOrWhiteSpace(output))
                return new List<DiscoveredPrinter>();

            using var doc = JsonDocument.Parse(output);
            var rows = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().ToList()
                : new List<JsonElement> { doc.RootElement };

            return rows.Select(row =>
            {
                var host = row.TryGetProperty("Host", out var h) && h.ValueKind == JsonValueKind.String ? h.GetString() : null;
                var port = row.TryGetProperty("Port", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : RawPrintPort;
                return new DiscoveredPrinter
                {
                    Name = row.GetProperty("Name").GetString() ?? "Printer",
                    IpAddress = string.IsNullOrWhiteSpace(host) ? null : host,
                    Port = port > 0 ? port : RawPrintPort,
                    Source = "installed"
                };
            }).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // never started or already gone
            }

            _logger.LogWarning(ex, "Installed printers could not be listed.");
            return new List<DiscoveredPrinter>();
        }
    }

    /// <summary>
    /// Probes port 9100 on every host of this machine's private /24 networks — thermal receipt
    /// and kitchen printers listen there for raw ESC/POS jobs.
    /// </summary>
    private async Task<List<DiscoveredPrinter>> ScanLocalNetworkAsync(CancellationToken cancellationToken)
    {
        var candidates = GetLocalSubnetHosts().ToList();
        var found = new List<DiscoveredPrinter>();
        var gate = new SemaphoreSlim(MaxParallelProbes);

        var probes = candidates.Select(async ip =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (await IsPortOpenAsync(ip, RawPrintPort, cancellationToken))
                {
                    lock (found)
                        found.Add(new DiscoveredPrinter
                        {
                            Name = $"Şəbəkə printeri {ip}",
                            IpAddress = ip.ToString(),
                            Port = RawPrintPort,
                            Source = "network"
                        });
                }
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(probes);
        return found.OrderBy(x => IPAddress.Parse(x.IpAddress!).GetAddressBytes()[3]).ToList();
    }

    private static IEnumerable<IPAddress> GetLocalSubnetHosts()
    {
        var seen = new HashSet<string>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up
                || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork || !IsPrivate(unicast.Address))
                    continue;

                var bytes = unicast.Address.GetAddressBytes();
                var prefix = $"{bytes[0]}.{bytes[1]}.{bytes[2]}";
                if (!seen.Add(prefix))
                    continue;

                for (var host = 1; host < 255; host++)
                {
                    if (host == bytes[3])
                        continue;
                    yield return IPAddress.Parse($"{prefix}.{host}");
                }
            }
        }
    }

    private static bool IsPrivate(IPAddress address)
    {
        var b = address.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168);
    }

    private static async Task<bool> IsPortOpenAsync(IPAddress ip, int port, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeoutMs);
        try
        {
            await client.ConnectAsync(ip, port, timeout.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}

namespace Application.Common.Interfaces.Abstracts.İnterfaces;

public interface INetworkPrinterService
{
    Task PrintAsync(string ipAddress, int port, string content, CancellationToken cancellationToken);
}

using Application.StoreMode.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API;

/// <summary>Real-time channel of the store screens; every connection joins the group of its company.</summary>
[Authorize]
public class StoreHub : Hub
{
    public static string Group(int companyId) => $"store-company-{companyId}";

    public override async Task OnConnectedAsync()
    {
        if (int.TryParse(Context.User?.FindFirst("companyId")?.Value, out var companyId) && companyId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, Group(companyId));
        await base.OnConnectedAsync();
    }
}

public sealed class SignalRStoreRealtime : IStoreRealtime
{
    private readonly IHubContext<StoreHub> _hub;

    public SignalRStoreRealtime(IHubContext<StoreHub> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(int companyId, string eventName, object payload, CancellationToken ct)
        => _hub.Clients.Group(StoreHub.Group(companyId)).SendAsync(eventName, payload, ct);
}

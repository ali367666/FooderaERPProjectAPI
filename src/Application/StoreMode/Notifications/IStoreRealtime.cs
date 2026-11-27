namespace Application.StoreMode.Notifications;

/// <summary>Pushes a store event to the browsers of a company the moment it happens (the screens still work without it, by polling).</summary>
public interface IStoreRealtime
{
    Task PublishAsync(int companyId, string eventName, object payload, CancellationToken ct);
}

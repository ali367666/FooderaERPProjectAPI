namespace Application.Common.Interfaces;

public interface IDeviceAccessService
{
    Task<AccessDecision> CheckAsync(int companyId, string? deviceKey, CancellationToken cancellationToken);

    /// <summary>Drop cached answers after the licence or a device of the company changed.</summary>
    void Invalidate(int companyId);
}

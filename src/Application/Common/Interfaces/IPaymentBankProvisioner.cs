namespace Application.Common.Interfaces;

public record PaymentBankStatus(string Name, bool Enabled);

/// <summary>Gives a company its payment banks. Only the platform SuperAdmin uses it.</summary>
public interface IPaymentBankProvisioner
{
    /// <summary>Every bank of the catalog and whether this company has it switched on.</summary>
    Task<List<PaymentBankStatus>> GetAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>
    /// Switches exactly these banks on for the company (creating or re-activating them) and the others off.
    /// A bank is never deleted: card payments already recorded against it keep their link.
    /// </summary>
    Task SetAsync(int companyId, IReadOnlyCollection<string> enabledNames, CancellationToken cancellationToken);
}

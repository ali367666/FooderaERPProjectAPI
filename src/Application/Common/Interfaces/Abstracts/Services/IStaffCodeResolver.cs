namespace Application.Common.Interfaces.Abstracts.Services;

public interface IStaffCodeResolver
{
    /// <summary>
    /// Finds the company staff member a POS code belongs to — the fixed code as typed, or for
    /// rotating-PIN roles today's ddMM followed by the code. Null when the code matches nobody,
    /// the account is inactive or locked out.
    /// </summary>
    Task<Domain.Entities.User?> ResolveAsync(int companyId, string submittedCode, CancellationToken cancellationToken);
}

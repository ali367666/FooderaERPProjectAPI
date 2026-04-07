namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    int CompanyId { get; }
}
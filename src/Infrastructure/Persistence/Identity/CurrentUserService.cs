using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int UserId
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
                return 0;

            var candidates = new[]
            {
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst("sub")?.Value,
                User.FindFirst("nameid")?.Value,
                User.FindFirst("uid")?.Value,
                User.FindFirst("userId")?.Value,
                User.FindFirst("UserId")?.Value,
            };

            foreach (var v in candidates)
            {
                if (int.TryParse(v, out var id) && id > 0)
                    return id;
            }

            return 0;
        }
    }

    public int CompanyId
    {
        get
        {
            var companyId =
                User?.FindFirst("companyId")?.Value
                ?? User?.FindFirst("CompanyId")?.Value
                ?? User?.FindFirst("company_id")?.Value
                ?? User?.FindFirst("Company_id")?.Value;

            return int.TryParse(companyId, out var parsedCompanyId) ? parsedCompanyId : 0;
        }
    }
}
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

    public int UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?
                .User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return userId != null ? int.Parse(userId) : 0;
        }
    }

    public int CompanyId
    {
        get
        {
            var companyId = _httpContextAccessor.HttpContext?
                .User?
                .FindFirst("companyId")?.Value;

            return companyId != null ? int.Parse(companyId) : 0;
        }
    }
}
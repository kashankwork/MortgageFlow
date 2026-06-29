using System.Security.Claims;
using MortgageFlow.Application.Users;

namespace MortgageFlow.Api.Auth;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public CurrentUser? User
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email);
            var role = principal.FindFirstValue(ClaimTypes.Role);

            return Guid.TryParse(idValue, out var userId) && email is not null && role is not null
                ? new CurrentUser(userId, email, role)
                : null;
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Infrastructure.Identity;

namespace MortgageFlow.Infrastructure.Authentication;

public sealed class AuthService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwtOptions;

    public AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            // Missing user and wrong password intentionally share the same public failure path.
            return AuthenticationResult.Failure(AuthenticationFailureReason.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return AuthenticationResult.Failure(AuthenticationFailureReason.InactiveAccount);
        }

        var passwordIsValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordIsValid)
        {
            // Identity performs the password hash verification; this code does not implement hashing.
            return AuthenticationResult.Failure(AuthenticationFailureReason.InvalidCredentials);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.SingleOrDefault() ?? string.Empty;
        var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var token = CreateAccessToken(user, role, expiresUtc);
        return AuthenticationResult.Success(
            new AuthenticatedUser(user.Id, user.Email ?? normalizedEmail, user.FullName, role, token, expiresUtc));
    }

    private string CreateAccessToken(ApplicationUser user, string role, DateTimeOffset expiresUtc)
    {
        // Keep tokens compact: identity, contact email, display name, and role are enough for API policies.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresUtc.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

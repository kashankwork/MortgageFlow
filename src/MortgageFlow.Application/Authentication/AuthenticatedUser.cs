namespace MortgageFlow.Application.Authentication;

/// <summary>
/// Public authentication shape returned to API clients after a successful login.
/// </summary>
public sealed record AuthenticatedUser(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string AccessToken,
    DateTimeOffset ExpiresUtc);

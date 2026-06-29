namespace MortgageFlow.Application.Authentication;

/// <summary>
/// Authenticates synthetic demo users without exposing Identity implementation details to API controllers.
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
}

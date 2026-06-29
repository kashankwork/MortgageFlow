namespace MortgageFlow.Application.Users;

/// <summary>
/// Provides the authenticated caller to application services without coupling them to ASP.NET Core claims APIs.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    CurrentUser? User { get; }
}

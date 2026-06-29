namespace MortgageFlow.Application.Authentication;

public sealed record AuthenticationResult
{
    private AuthenticationResult(AuthenticatedUser? user, AuthenticationFailureReason? failureReason)
    {
        User = user;
        FailureReason = failureReason;
    }

    public bool Succeeded => User is not null;

    public AuthenticatedUser? User { get; }

    public AuthenticationFailureReason? FailureReason { get; }

    public static AuthenticationResult Success(AuthenticatedUser user) => new(user, null);

    public static AuthenticationResult Failure(AuthenticationFailureReason reason) => new(null, reason);
}

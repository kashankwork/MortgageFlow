namespace MortgageFlow.Application.Users;

public sealed record CurrentUser(Guid UserId, string Email, string Role);

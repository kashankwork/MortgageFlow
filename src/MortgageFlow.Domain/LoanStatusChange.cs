namespace MortgageFlow.Domain;

public sealed record LoanStatusChange(
    LoanStatus PreviousStatus,
    LoanStatus NewStatus,
    Guid ActorId,
    string? Reason,
    DateTime ChangedUtc);

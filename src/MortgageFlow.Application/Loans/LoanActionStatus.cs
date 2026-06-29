namespace MortgageFlow.Application.Loans;

public enum LoanActionStatus
{
    Success = 0,
    NotFound = 1,
    Forbidden = 2,
    ValidationFailed = 3,
    Conflict = 4,
    InvalidTransition = 5
}

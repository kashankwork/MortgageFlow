namespace MortgageFlow.Application.Loans;

public sealed record LoanActionResult<T>
{
    private LoanActionResult(
        LoanActionStatus status,
        T? value,
        string? message,
        IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Status = status;
        Value = value;
        Message = message;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
    }

    public LoanActionStatus Status { get; }

    public T? Value { get; }

    public string? Message { get; }

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public static LoanActionResult<T> Success(T value) => new(LoanActionStatus.Success, value, null, null);

    public static LoanActionResult<T> NotFound() => new(LoanActionStatus.NotFound, default, null, null);

    public static LoanActionResult<T> Forbidden(string message) => new(LoanActionStatus.Forbidden, default, message, null);

    public static LoanActionResult<T> Conflict(string message) => new(LoanActionStatus.Conflict, default, message, null);

    public static LoanActionResult<T> InvalidTransition(string message) =>
        new(LoanActionStatus.InvalidTransition, default, message, null);

    public static LoanActionResult<T> ValidationFailed(IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(LoanActionStatus.ValidationFailed, default, "Loan request validation failed.", validationErrors);
}

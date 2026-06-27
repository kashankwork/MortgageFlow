using MortgageFlow.Domain;

namespace MortgageFlow.Application.Loans;

public sealed record CreateLoanRequest(
    decimal RequestedAmount,
    LoanPurpose? LoanPurpose,
    decimal? InterestRatePercent,
    int? TermMonths,
    BorrowerDto? Borrower,
    PropertyDto? Property);

public sealed record UpdateLoanRequest(
    string RowVersion,
    decimal RequestedAmount,
    LoanPurpose? LoanPurpose,
    decimal? InterestRatePercent,
    int? TermMonths,
    BorrowerDto? Borrower,
    PropertyDto? Property);

public sealed record TransitionLoanRequest(LoanStatus NextStatus, string? Reason, string RowVersion);

public sealed record BorrowerDto(string FullName, string Email, decimal AnnualIncome);

public sealed record PropertyDto(
    string StreetAddress,
    string City,
    string State,
    string PostalCode,
    decimal EstimatedValue,
    OccupancyType OccupancyType);

public sealed record LoanDetailResponse(
    Guid Id,
    string LoanNumber,
    Guid BrokerId,
    Guid? AssigneeId,
    LoanStatus Status,
    BusinessPriority BusinessPriority,
    decimal RequestedAmount,
    LoanPurpose? LoanPurpose,
    decimal? InterestRatePercent,
    int? TermMonths,
    BorrowerDto? Borrower,
    PropertyDto? Property,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    DateTime? SubmittedUtc,
    string RowVersion);

public sealed record LoanListItemResponse(
    Guid Id,
    string LoanNumber,
    LoanStatus Status,
    decimal RequestedAmount,
    string? BorrowerName,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    DateTime? SubmittedUtc);

public sealed record LoanStatusHistoryResponse(
    LoanStatus PreviousStatus,
    LoanStatus NewStatus,
    Guid ActorId,
    string? Reason,
    DateTime ChangedUtc);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

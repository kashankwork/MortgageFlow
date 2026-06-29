using MortgageFlow.Domain;

namespace MortgageFlow.Application.Assignments;

public sealed record AssignmentDecisionResponse(
    Guid LoanId,
    string LoanNumber,
    bool Assigned,
    Guid? AssignmentId,
    Guid? AssigneeId,
    string? AssigneeName,
    string Result,
    int PriorityScore,
    decimal? NormalizedLoad,
    string RowVersion);

public sealed record ManualReassignmentRequest(Guid NewAssigneeId, string Reason, string RowVersion);

public sealed record PriorityUpdateRequest(BusinessPriority BusinessPriority, string Reason, string RowVersion);

public sealed record PriorityUpdateResponse(
    Guid LoanId,
    string LoanNumber,
    BusinessPriority BusinessPriority,
    int PriorityScore,
    string RowVersion);

public sealed record QueueItemResponse(
    Guid LoanId,
    string LoanNumber,
    LoanStatus Status,
    BusinessPriority BusinessPriority,
    int PriorityScore,
    DateTime? EarliestDueUtc,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    DateTime? SubmittedUtc);

public sealed record AssignmentCandidateResponse(
    Guid EmployeeId,
    string FullName,
    string Role,
    int CapacityPoints,
    int OpenTaskWeight,
    int ActiveLoanCount,
    decimal NormalizedLoad);

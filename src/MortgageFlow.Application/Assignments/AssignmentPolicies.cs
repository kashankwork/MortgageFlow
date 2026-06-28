using MortgageFlow.Application.Loans;
using MortgageFlow.Domain;

namespace MortgageFlow.Application.Assignments;

/// <summary>
/// Calculates work priority independently from employee assignment.
/// </summary>
public interface ILoanPriorityCalculator
{
    int Calculate(LoanPriorityInput input);
}

public interface IEmployeeEligibilityPolicy
{
    bool IsEligible(EmployeeAssignmentCandidate candidate, LoanAssignmentContext context);
}

public interface IEmployeeWorkloadCalculator
{
    EmployeeWorkload Calculate(EmployeeAssignmentCandidate candidate);
}

public interface IRoundRobinTieBreaker
{
    Task<EmployeeAssignmentCandidate> SelectAsync(
        string routingKey,
        IReadOnlyCollection<EmployeeAssignmentCandidate> candidates,
        CancellationToken cancellationToken);
}

public interface IAssignmentStrategy
{
    Task<AssignmentStrategyResult> SelectAssigneeAsync(
        LoanAssignmentContext context,
        IReadOnlyCollection<EmployeeAssignmentCandidate> candidates,
        CancellationToken cancellationToken);
}

public interface ILoanAssignmentService
{
    Task<LoanActionResult<AssignmentDecisionResponse>> AssignAutomaticallyAsync(
        Guid loanId,
        CancellationToken cancellationToken);

    Task<LoanActionResult<AssignmentDecisionResponse>> ReassignAsync(
        Guid loanId,
        ManualReassignmentRequest request,
        CancellationToken cancellationToken);

    Task<LoanActionResult<PriorityUpdateResponse>> UpdatePriorityAsync(
        Guid loanId,
        PriorityUpdateRequest request,
        CancellationToken cancellationToken);
}

public interface IQueueService
{
    Task<LoanActionResult<PagedResult<QueueItemResponse>>> GetMyQueueAsync(
        int page,
        int pageSize,
        LoanStatus? status,
        CancellationToken cancellationToken);

    Task<LoanActionResult<PagedResult<QueueItemResponse>>> GetTeamQueueAsync(
        int page,
        int pageSize,
        LoanStatus? status,
        CancellationToken cancellationToken);
}

public sealed record LoanPriorityInput(
    BusinessPriority BusinessPriority,
    LoanStatus Status,
    DateTime CreatedUtc,
    DateTime? SubmittedUtc,
    DateTime? EarliestDueUtc,
    DateTime NowUtc);

public sealed record LoanAssignmentContext(
    Guid LoanId,
    string LoanNumber,
    LoanStatus Status,
    Guid TeamId,
    string RequiredRole,
    string RequiredSkill,
    int PriorityScore);

public sealed record EmployeeAssignmentCandidate(
    Guid EmployeeId,
    string FullName,
    string Role,
    Guid TeamId,
    bool IsActive,
    bool IsAvailable,
    int CapacityPoints,
    IReadOnlyCollection<string> SkillTags,
    int OpenTaskWeight,
    int ActiveLoanCount);

public sealed record EmployeeWorkload(int WeightedLoad, decimal NormalizedLoad, bool HasRemainingCapacity);

public sealed record AssignmentStrategyResult(
    bool Assigned,
    EmployeeAssignmentCandidate? Candidate,
    EmployeeWorkload? Workload,
    string Reason);

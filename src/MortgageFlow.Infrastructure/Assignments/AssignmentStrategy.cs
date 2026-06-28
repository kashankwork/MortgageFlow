using MortgageFlow.Application.Assignments;

namespace MortgageFlow.Infrastructure.Assignments;

public sealed class AssignmentStrategy : IAssignmentStrategy
{
    private readonly IEmployeeEligibilityPolicy _eligibilityPolicy;
    private readonly IEmployeeWorkloadCalculator _workloadCalculator;
    private readonly IRoundRobinTieBreaker _tieBreaker;

    public AssignmentStrategy(
        IEmployeeEligibilityPolicy eligibilityPolicy,
        IEmployeeWorkloadCalculator workloadCalculator,
        IRoundRobinTieBreaker tieBreaker)
    {
        _eligibilityPolicy = eligibilityPolicy;
        _workloadCalculator = workloadCalculator;
        _tieBreaker = tieBreaker;
    }

    public async Task<AssignmentStrategyResult> SelectAssigneeAsync(
        LoanAssignmentContext context,
        IReadOnlyCollection<EmployeeAssignmentCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var eligible = candidates
            .Where(candidate => _eligibilityPolicy.IsEligible(candidate, context))
            .Select(candidate => new CandidateLoad(candidate, _workloadCalculator.Calculate(candidate)))
            .ToList();

        if (eligible.Count == 0)
        {
            return new AssignmentStrategyResult(
                Assigned: false,
                Candidate: null,
                Workload: null,
                Reason: "No eligible employee is available for this loan stage.");
        }

        var lowestLoad = eligible.Min(candidate => candidate.Workload.NormalizedLoad);
        var bestCandidates = eligible
            .Where(candidate => candidate.Workload.NormalizedLoad == lowestLoad)
            .ToList();

        var selected = bestCandidates.Count == 1
            ? bestCandidates[0].Candidate
            : await _tieBreaker.SelectAsync(
                AssignmentConstants.CreateRoutingKey(context.TeamId, context.RequiredRole, context.RequiredSkill),
                bestCandidates.Select(candidate => candidate.Candidate).ToList(),
                cancellationToken);

        var selectedLoad = bestCandidates.Single(candidate => candidate.Candidate.EmployeeId == selected.EmployeeId).Workload;
        return new AssignmentStrategyResult(
            Assigned: true,
            Candidate: selected,
            Workload: selectedLoad,
            Reason: "Selected lowest normalized eligible employee.");
    }

    private sealed record CandidateLoad(EmployeeAssignmentCandidate Candidate, EmployeeWorkload Workload);
}

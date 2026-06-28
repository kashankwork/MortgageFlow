using MortgageFlow.Application.Assignments;

namespace MortgageFlow.Infrastructure.Assignments;

public sealed class EmployeeEligibilityPolicy : IEmployeeEligibilityPolicy
{
    private readonly IEmployeeWorkloadCalculator _workloadCalculator;

    public EmployeeEligibilityPolicy(IEmployeeWorkloadCalculator workloadCalculator)
    {
        _workloadCalculator = workloadCalculator;
    }

    public bool IsEligible(EmployeeAssignmentCandidate candidate, LoanAssignmentContext context)
    {
        if (!candidate.IsActive || !candidate.IsAvailable || candidate.CapacityPoints <= 0)
        {
            return false;
        }

        if (candidate.Role != context.RequiredRole || candidate.TeamId != context.TeamId)
        {
            return false;
        }

        var hasRequiredSkill = candidate.SkillTags.Any(skill =>
            string.Equals(skill, context.RequiredSkill, StringComparison.OrdinalIgnoreCase));
        if (!hasRequiredSkill)
        {
            return false;
        }

        return _workloadCalculator.Calculate(candidate).HasRemainingCapacity;
    }
}

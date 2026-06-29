using Microsoft.Extensions.Options;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Assignments;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class AssignmentPolicyTests
{
    [Fact]
    public void PriorityScore_UsesSingleDueDateBucketAndClamps()
    {
        var calculator = new LoanPriorityCalculator(Options.Create(new AssignmentOptions()));
        var now = DateTime.UtcNow;

        var score = calculator.Calculate(new LoanPriorityInput(
            BusinessPriority.Urgent,
            LoanStatus.MoreInformationRequired,
            now.AddDays(-30),
            now.AddDays(-20),
            now.AddHours(-2),
            now));

        Assert.Equal(100, score);
    }

    [Fact]
    public void Eligibility_ExcludesInactiveUnavailableWrongRoleWrongSkillCrossTeamAndOverCapacity()
    {
        var options = Options.Create(new AssignmentOptions());
        var workload = new EmployeeWorkloadCalculator(options);
        var policy = new EmployeeEligibilityPolicy(workload);
        var teamId = Guid.NewGuid();
        var context = new LoanAssignmentContext(
            Guid.NewGuid(),
            "MF-800001",
            LoanStatus.Submitted,
            teamId,
            MortgageFlowRoles.Processor,
            "processing",
            20);

        Assert.True(policy.IsEligible(Candidate(teamId), context));
        Assert.False(policy.IsEligible(Candidate(teamId, isActive: false), context));
        Assert.False(policy.IsEligible(Candidate(teamId, isAvailable: false), context));
        Assert.False(policy.IsEligible(Candidate(teamId, role: MortgageFlowRoles.Underwriter), context));
        Assert.False(policy.IsEligible(Candidate(teamId, skill: "underwriting"), context));
        Assert.False(policy.IsEligible(Candidate(Guid.NewGuid()), context));
        Assert.False(policy.IsEligible(Candidate(teamId, openTaskWeight: 10, activeLoanCount: 1), context));
    }

    [Fact]
    public async Task Strategy_SelectsLowestNormalizedLoad()
    {
        var options = Options.Create(new AssignmentOptions());
        var workload = new EmployeeWorkloadCalculator(options);
        var strategy = new AssignmentStrategy(
            new EmployeeEligibilityPolicy(workload),
            workload,
            new StaticTieBreaker());
        var teamId = Guid.NewGuid();
        var context = new LoanAssignmentContext(
            Guid.NewGuid(),
            "MF-800002",
            LoanStatus.Submitted,
            teamId,
            MortgageFlowRoles.Processor,
            "processing",
            20);
        var busierRawCountButLowerRatio = Candidate(teamId, capacity: 20, openTaskWeight: 6, activeLoanCount: 2);
        var fewerRawItemsButHigherRatio = Candidate(teamId, capacity: 8, openTaskWeight: 4, activeLoanCount: 1);

        var result = await strategy.SelectAssigneeAsync(
            context,
            [fewerRawItemsButHigherRatio, busierRawCountButLowerRatio],
            CancellationToken.None);

        Assert.True(result.Assigned);
        Assert.Equal(busierRawCountButLowerRatio.EmployeeId, result.Candidate?.EmployeeId);
    }

    [Fact]
    public async Task Strategy_ReturnsTypedNoCandidateResult()
    {
        var options = Options.Create(new AssignmentOptions());
        var workload = new EmployeeWorkloadCalculator(options);
        var strategy = new AssignmentStrategy(
            new EmployeeEligibilityPolicy(workload),
            workload,
            new StaticTieBreaker());
        var context = new LoanAssignmentContext(
            Guid.NewGuid(),
            "MF-800003",
            LoanStatus.Submitted,
            Guid.NewGuid(),
            MortgageFlowRoles.Processor,
            "processing",
            20);

        var result = await strategy.SelectAssigneeAsync(context, [], CancellationToken.None);

        Assert.False(result.Assigned);
        Assert.Null(result.Candidate);
    }

    private static EmployeeAssignmentCandidate Candidate(
        Guid teamId,
        string role = MortgageFlowRoles.Processor,
        string skill = "processing",
        bool isActive = true,
        bool isAvailable = true,
        int capacity = 12,
        int openTaskWeight = 0,
        int activeLoanCount = 0)
    {
        return new EmployeeAssignmentCandidate(
            Guid.NewGuid(),
            $"Candidate {Guid.NewGuid():N}",
            role,
            teamId,
            isActive,
            isAvailable,
            capacity,
            [skill],
            openTaskWeight,
            activeLoanCount);
    }

    private sealed class StaticTieBreaker : IRoundRobinTieBreaker
    {
        public Task<EmployeeAssignmentCandidate> SelectAsync(
            string routingKey,
            IReadOnlyCollection<EmployeeAssignmentCandidate> candidates,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(candidates.First());
        }
    }
}

using Microsoft.Extensions.Options;
using MortgageFlow.Application.Assignments;

namespace MortgageFlow.Infrastructure.Assignments;

public sealed class EmployeeWorkloadCalculator : IEmployeeWorkloadCalculator
{
    private readonly AssignmentOptions _options;

    public EmployeeWorkloadCalculator(IOptions<AssignmentOptions> options)
    {
        _options = options.Value;
    }

    public EmployeeWorkload Calculate(EmployeeAssignmentCandidate candidate)
    {
        if (candidate.CapacityPoints <= 0)
        {
            return new EmployeeWorkload(int.MaxValue, decimal.MaxValue, HasRemainingCapacity: false);
        }

        var weightedLoad = candidate.OpenTaskWeight + (_options.ActiveLoanWeight * candidate.ActiveLoanCount);
        var normalizedLoad = decimal.Round(weightedLoad / (decimal)candidate.CapacityPoints, 4);

        return new EmployeeWorkload(
            weightedLoad,
            normalizedLoad,
            HasRemainingCapacity: weightedLoad < candidate.CapacityPoints);
    }
}

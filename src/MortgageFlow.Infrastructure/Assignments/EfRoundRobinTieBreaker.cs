using Microsoft.EntityFrameworkCore;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Infrastructure.Persistence;
using MortgageFlow.Infrastructure.Persistence.Entities;

namespace MortgageFlow.Infrastructure.Assignments;

public sealed class EfRoundRobinTieBreaker : IRoundRobinTieBreaker
{
    private readonly MortgageFlowDbContext _dbContext;

    public EfRoundRobinTieBreaker(MortgageFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmployeeAssignmentCandidate> SelectAsync(
        string routingKey,
        IReadOnlyCollection<EmployeeAssignmentCandidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("At least one candidate is required for tie-breaking.");
        }

        var ordered = candidates
            .OrderBy(candidate => candidate.FullName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.EmployeeId)
            .ToList();

        var cursor = await _dbContext.RoundRobinCursors
            .SingleOrDefaultAsync(item => item.RoutingKey == routingKey, cancellationToken);
        if (cursor is null)
        {
            cursor = new RoundRobinCursor
            {
                Id = Guid.NewGuid(),
                RoutingKey = routingKey
            };
            _dbContext.RoundRobinCursors.Add(cursor);
        }

        var previousIndex = cursor.LastSelectedEmployeeId is null
            ? -1
            : ordered.FindIndex(candidate => candidate.EmployeeId == cursor.LastSelectedEmployeeId.Value);
        var selected = ordered[(previousIndex + 1) % ordered.Count];
        cursor.LastSelectedEmployeeId = selected.EmployeeId;

        return selected;
    }
}

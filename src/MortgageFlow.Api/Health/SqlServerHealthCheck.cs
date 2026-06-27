using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MortgageFlow.Infrastructure.Persistence;

namespace MortgageFlow.Api.Health;

public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly MortgageFlowDbContext _dbContext;

    public SqlServerHealthCheck(MortgageFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("SQL Server is reachable.")
            : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
    }
}

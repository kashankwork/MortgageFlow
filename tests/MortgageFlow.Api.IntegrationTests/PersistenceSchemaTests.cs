using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MortgageFlow.Infrastructure.Persistence;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class PersistenceSchemaTests : IClassFixture<MortgageFlowApiFactory>
{
    private readonly MortgageFlowApiFactory _factory;

    public PersistenceSchemaTests(MortgageFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InitialMigration_CreatesExpectedTables()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();

        var tables = await dbContext.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
            .ToListAsync();

        // This is a smoke test for the real migration output, not a replacement for reviewing the migration file.
        Assert.Contains("AspNetUsers", tables);
        Assert.Contains("AspNetRoles", tables);
        Assert.Contains("LoanApplications", tables);
        Assert.Contains("LoanStatusHistory", tables);
        Assert.Contains("LoanAssignments", tables);
        Assert.Contains("AuditLogs", tables);
        Assert.Contains("EmployeeSkills", tables);
        Assert.Contains("WorkflowTasks", tables);
        Assert.Contains("RoundRobinCursors", tables);
    }
}

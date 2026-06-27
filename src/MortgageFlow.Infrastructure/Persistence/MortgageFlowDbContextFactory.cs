using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MortgageFlow.Infrastructure.Persistence;

public sealed class MortgageFlowDbContextFactory : IDesignTimeDbContextFactory<MortgageFlowDbContext>
{
    public MortgageFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required for design-time EF commands.");

        var options = new DbContextOptionsBuilder<MortgageFlowDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new MortgageFlowDbContext(options);
    }
}

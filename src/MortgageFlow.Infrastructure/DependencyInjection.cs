using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Application.Loans;
using MortgageFlow.Infrastructure.Assignments;
using MortgageFlow.Infrastructure.Authentication;
using MortgageFlow.Infrastructure.Identity;
using MortgageFlow.Infrastructure.Loans;
using MortgageFlow.Infrastructure.Persistence;

namespace MortgageFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AssignmentOptions>(configuration.GetSection(AssignmentOptions.SectionName));

        services.AddDbContext<MortgageFlowDbContext>(options => options.UseSqlServer(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MortgageFlowDbContext>();

        services.AddScoped<IAuthenticationService, AuthService>();
        services.AddScoped<ILoanWorkflowService, EfLoanWorkflowService>();
        services.AddScoped<ILoanPriorityCalculator, LoanPriorityCalculator>();
        services.AddScoped<IEmployeeEligibilityPolicy, EmployeeEligibilityPolicy>();
        services.AddScoped<IEmployeeWorkloadCalculator, EmployeeWorkloadCalculator>();
        services.AddScoped<IRoundRobinTieBreaker, EfRoundRobinTieBreaker>();
        services.AddScoped<IAssignmentStrategy, AssignmentStrategy>();
        services.AddScoped<ILoanAssignmentService, EfLoanAssignmentService>();
        services.AddScoped<IQueueService, EfQueueService>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}

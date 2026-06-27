using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MortgageFlow.Api.Auth;
using MortgageFlow.Api.Health;
using MortgageFlow.Api.OpenApi;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Application.Users;
using MortgageFlow.Infrastructure;
using MortgageFlow.Infrastructure.Authentication;
using MortgageFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Infrastructure owns SQL Server, EF Core, Identity, and JWT token creation.
// The API wires those services in but keeps business/application contracts separate.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMortgageFlowAuthorization();
builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sql-server");
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { status = "Live" }))
    .WithName("LiveHealthCheck")
    .AllowAnonymous();

// Readiness is separate from liveness: the process can be alive even when SQL is unavailable.
// Docker, CI, and future deployments can use this endpoint before sending real traffic.
app.MapHealthChecks("/health/ready")
    .WithName("ReadyHealthCheck")
    .AllowAnonymous();

await app.ApplyDevelopmentDatabaseSetupAsync();

app.Run();

public partial class Program
{
}

internal static class ProgramConfiguration
{
    /// <summary>
    /// Configures JWT bearer validation for tokens issued by the infrastructure authentication service.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is required.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 characters.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Validate all three core trust checks: who issued it, who it is for, and who signed it.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }

    public static IServiceCollection AddMortgageFlowAuthorization(this IServiceCollection services)
    {
        // Policies are named once and reused by controllers/tests so role checks stay consistent.
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Broker, policy => policy.RequireRole(MortgageFlowRoles.Broker))
            .AddPolicy(AuthorizationPolicies.Processor, policy => policy.RequireRole(MortgageFlowRoles.Processor))
            .AddPolicy(AuthorizationPolicies.Underwriter, policy => policy.RequireRole(MortgageFlowRoles.Underwriter))
            .AddPolicy(AuthorizationPolicies.TeamLead, policy => policy.RequireRole(MortgageFlowRoles.TeamLead));

        return services;
    }

    public static async Task ApplyDevelopmentDatabaseSetupAsync(this WebApplication app)
    {
        var applyMigrations = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
        var hasSeedPassword = !string.IsNullOrWhiteSpace(app.Configuration["Seed:DemoPassword"]);
        var shouldSeed = hasSeedPassword && (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Demo"));

        // Production should apply migrations through release automation, not silently at startup.
        // Local/demo seeding only runs when an explicit synthetic demo password is supplied.
        if (!applyMigrations && !shouldSeed)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();

        if (applyMigrations)
        {
            await dbContext.Database.MigrateAsync();
        }

        if (shouldSeed)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
            await seeder.SeedAsync(CancellationToken.None);
        }
    }
}

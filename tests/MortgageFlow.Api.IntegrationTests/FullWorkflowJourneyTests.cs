using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Application.Loans;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Persistence;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class FullWorkflowJourneyTests : IClassFixture<MortgageFlowApiFactory>
{
    private readonly MortgageFlowApiFactory _factory;

    public FullWorkflowJourneyTests(MortgageFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FourRoleJourney_CompletesApprovedLoanWithHistoryAssignmentsAuditAndCorrelation()
    {
        var correlationId = $"journey-{Guid.NewGuid():N}";
        var broker = await CreateAuthorizedClientAsync("broker@example.test", correlationId);
        var teamLead = await CreateAuthorizedClientAsync("teamlead@example.test", correlationId);

        var created = await ReadSuccessAsync<LoanDetailResponse>(await broker.PostAsJsonAsync(
            "/api/v1/loans",
            CompleteCreateRequest("Synthetic Journey Borrower")));
        var updated = await ReadSuccessAsync<LoanDetailResponse>(await broker.PutAsJsonAsync(
            $"/api/v1/loans/{created.Id}",
            CompleteUpdateRequest(created.RowVersion, "Synthetic Journey Updated Borrower")));
        var submitted = await ReadSuccessAsync<LoanDetailResponse>(await broker.PostAsJsonAsync(
            $"/api/v1/loans/{created.Id}/transitions",
            new TransitionLoanRequest(LoanStatus.Submitted, null, updated.RowVersion)));

        var assigned = await ReadSuccessAsync<AssignmentDecisionResponse>(
            await teamLead.PostAsync($"/api/v1/loans/{created.Id}/assign", content: null));
        Assert.True(assigned.Assigned);

        var processorEmail = await FindUserEmailAsync(assigned.AssigneeId!.Value);
        var processor = await CreateAuthorizedClientAsync(processorEmail, correlationId);
        var processing = await ReadSuccessAsync<LoanDetailResponse>(await processor.PostAsJsonAsync(
            $"/api/v1/loans/{created.Id}/transitions",
            new TransitionLoanRequest(LoanStatus.Processing, null, assigned.RowVersion)));
        var underwriting = await ReadSuccessAsync<LoanDetailResponse>(await processor.PostAsJsonAsync(
            $"/api/v1/loans/{created.Id}/transitions",
            new TransitionLoanRequest(LoanStatus.Underwriting, "Synthetic checklist complete.", processing.RowVersion)));

        var underwriterId = await FindUserIdAsync("underwriter.secondary@example.test");
        var reassigned = await ReadSuccessAsync<AssignmentDecisionResponse>(await teamLead.PostAsJsonAsync(
            $"/api/v1/loans/{created.Id}/reassign",
            new ManualReassignmentRequest(underwriterId, "Move approved demo flow to underwriting queue.", underwriting.RowVersion)));

        var underwriter = await CreateAuthorizedClientAsync("underwriter.secondary@example.test", correlationId);
        var approved = await ReadSuccessAsync<LoanDetailResponse>(await underwriter.PostAsJsonAsync(
            $"/api/v1/loans/{created.Id}/transitions",
            new TransitionLoanRequest(LoanStatus.Approved, "Synthetic approval rationale.", reassigned.RowVersion)));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var historyCount = await dbContext.LoanApplications
            .Where(loan => loan.Id == created.Id)
            .SelectMany(loan => loan.StatusHistory)
            .CountAsync();
        var assignmentCount = await dbContext.LoanAssignments.CountAsync(item => item.LoanApplicationId == created.Id);
        var audits = await dbContext.AuditLogs
            .Where(item => item.EntityId == created.Id.ToString())
            .OrderBy(item => item.CreatedUtc)
            .ToListAsync();

        Assert.Equal(LoanStatus.Approved, approved.Status);
        Assert.Equal(underwriterId, approved.AssigneeId);
        Assert.Equal(4, historyCount);
        Assert.Equal(2, assignmentCount);
        Assert.Contains(audits, item => item.Action == "LoanCreated");
        Assert.Contains(audits, item => item.Action == "LoanAssigned");
        Assert.Contains(audits, item => item.Action == "LoanReassigned");
        Assert.Contains(audits, item => item.Action == "LoanTransitioned");
        Assert.All(audits, audit =>
        {
            Assert.Equal(correlationId, audit.CorrelationId);
            Assert.DoesNotContain("Synthetic Journey Updated Borrower", audit.Summary);
            Assert.DoesNotContain("journey.borrower@example.test", audit.Summary);
            Assert.DoesNotContain("123 Birmingham", audit.Summary);
        });
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(string email, string correlationId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = MortgageFlowApiFactory.DemoPassword });
        var login = await ReadSuccessAsync<LoginResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<T>();
        return value ?? throw new InvalidOperationException($"Response did not contain {typeof(T).Name}.");
    }

    private async Task<Guid> FindUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        return await dbContext.Users
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private async Task<string> FindUserEmailAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        return await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.Email!)
            .SingleAsync();
    }

    private static CreateLoanRequest CompleteCreateRequest(string borrowerName)
    {
        return new CreateLoanRequest(
            310_000,
            LoanPurpose.Purchase,
            6.625m,
            360,
            new BorrowerDto(borrowerName, "journey.borrower@example.test", 142_000),
            new PropertyDto(
                "123 Birmingham Demo Lane",
                "Birmingham",
                "MI",
                "48009",
                425_000,
                OccupancyType.PrimaryResidence));
    }

    private static UpdateLoanRequest CompleteUpdateRequest(string rowVersion, string borrowerName)
    {
        var create = CompleteCreateRequest(borrowerName);
        return new UpdateLoanRequest(
            rowVersion,
            create.RequestedAmount,
            create.LoanPurpose,
            create.InterestRatePercent,
            create.TermMonths,
            create.Borrower,
            create.Property);
    }

    private sealed record LoginResponse(string AccessToken);
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Application.Loans;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Assignments;
using MortgageFlow.Infrastructure.Persistence;
using MortgageFlow.Infrastructure.Persistence.Entities;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class AssignmentEndpointTests : IClassFixture<MortgageFlowApiFactory>
{
    private static int _loanNumberSequence = 810000;

    private readonly MortgageFlowApiFactory _factory;

    public AssignmentEndpointTests(MortgageFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AutomaticAssignment_SubmittedLoan_SelectsBestProcessorAndWritesRecords()
    {
        var teamLead = await CreateAuthorizedClientAsync("teamlead@example.test");
        var loanId = await FindLoanIdAsync("MF-900003");
        var expectedProcessor = await FindUserIdAsync("processor.secondary@example.test");

        var response = await teamLead.PostAsync($"/api/v1/loans/{loanId}/assign", content: null);
        var assignment = await ReadSuccessAsync<AssignmentDecisionResponse>(response);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var recordCount = await dbContext.LoanAssignments.CountAsync(item => item.LoanApplicationId == loanId);
        var auditCount = await dbContext.AuditLogs.CountAsync(item => item.EntityId == loanId.ToString() && item.Action == "LoanAssigned");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(assignment.Assigned);
        Assert.Equal(expectedProcessor, assignment.AssigneeId);
        Assert.Equal(1, recordCount);
        Assert.Equal(1, auditCount);
    }

    [Fact]
    public async Task AutomaticAssignment_UnderwritingLoan_SelectsBestUnderwriter()
    {
        var teamLead = await CreateAuthorizedClientAsync("teamlead@example.test");
        var loanId = await FindLoanIdAsync("MF-900004");
        var expectedUnderwriter = await FindUserIdAsync("underwriter.secondary@example.test");

        var response = await teamLead.PostAsync($"/api/v1/loans/{loanId}/assign", content: null);
        var assignment = await ReadSuccessAsync<AssignmentDecisionResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(assignment.Assigned);
        Assert.Equal(expectedUnderwriter, assignment.AssigneeId);
    }

    [Fact]
    public async Task AutomaticAssignment_WithNoEligibleCandidate_ReturnsTypedUnassignedResult()
    {
        var teamLead = await CreateAuthorizedClientAsync("teamlead@example.test");
        var loanId = await InsertLoanAsync(LoanStatus.Submitted, assigneeId: null);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var processors = await dbContext.Users
            .Where(user => user.Email!.Contains("processor"))
            .ToListAsync();
        foreach (var processor in processors)
        {
            processor.IsAvailable = false;
        }

        await dbContext.SaveChangesAsync();

        try
        {
            var response = await teamLead.PostAsync($"/api/v1/loans/{loanId}/assign", content: null);
            var assignment = await ReadSuccessAsync<AssignmentDecisionResponse>(response);
            var recordCount = await dbContext.LoanAssignments.CountAsync(item => item.LoanApplicationId == loanId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(assignment.Assigned);
            Assert.Null(assignment.AssigneeId);
            Assert.Equal(0, recordCount);
        }
        finally
        {
            foreach (var processor in processors)
            {
                processor.IsAvailable = true;
            }

            await dbContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task ManualReassignment_RequiresTeamLeadReasonAndEligibleEmployee()
    {
        var teamLead = await CreateAuthorizedClientAsync("teamlead@example.test");
        var broker = await CreateAuthorizedClientAsync("broker@example.test");
        var loanId = await FindLoanIdAsync("MF-900001");
        var detail = await ReadSuccessAsync<LoanDetailResponse>(await teamLead.GetAsync($"/api/v1/loans/{loanId}"));
        var processor2 = await FindUserIdAsync("processor.secondary@example.test");
        var underwriter = await FindUserIdAsync("underwriter@example.test");

        var brokerResponse = await broker.PostAsJsonAsync(
            $"/api/v1/loans/{loanId}/reassign",
            new ManualReassignmentRequest(processor2, "Broker cannot reassign.", detail.RowVersion));
        var missingReasonResponse = await teamLead.PostAsJsonAsync(
            $"/api/v1/loans/{loanId}/reassign",
            new ManualReassignmentRequest(processor2, "", detail.RowVersion));
        var wrongRoleResponse = await teamLead.PostAsJsonAsync(
            $"/api/v1/loans/{loanId}/reassign",
            new ManualReassignmentRequest(underwriter, "Wrong role for submitted loan.", detail.RowVersion));
        var validResponse = await teamLead.PostAsJsonAsync(
            $"/api/v1/loans/{loanId}/reassign",
            new ManualReassignmentRequest(processor2, "Balance active processor queue.", detail.RowVersion));
        var reassigned = await ReadSuccessAsync<AssignmentDecisionResponse>(validResponse);

        Assert.Equal(HttpStatusCode.Forbidden, brokerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingReasonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrongRoleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        Assert.Equal(processor2, reassigned.AssigneeId);
    }

    [Fact]
    public async Task PriorityUpdate_RequiresTeamLeadAndWritesAudit()
    {
        var teamLead = await CreateAuthorizedClientAsync("teamlead@example.test");
        var loanId = await FindLoanIdAsync("MF-900001");
        var detail = await ReadSuccessAsync<LoanDetailResponse>(await teamLead.GetAsync($"/api/v1/loans/{loanId}"));

        var response = await teamLead.PatchAsJsonAsync(
            $"/api/v1/loans/{loanId}/priority",
            new PriorityUpdateRequest(BusinessPriority.Urgent, "Escalate synthetic service-level review.", detail.RowVersion));
        var priority = await ReadSuccessAsync<PriorityUpdateResponse>(response);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var auditCount = await dbContext.AuditLogs.CountAsync(item =>
            item.EntityId == loanId.ToString() && item.Action == "LoanPriorityUpdated");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(BusinessPriority.Urgent, priority.BusinessPriority);
        Assert.True(priority.PriorityScore >= 30);
        Assert.Equal(1, auditCount);
    }

    [Fact]
    public async Task Queues_ReturnAuthorizedStablePagedResults()
    {
        var processorId = await FindUserIdAsync("processor@example.test");
        var assignedLoanId = await InsertLoanAsync(LoanStatus.Submitted, processorId);
        var processor = await CreateAuthorizedClientAsync("processor@example.test");
        var teamLead = await CreateAuthorizedClientAsync("teamlead@example.test");

        var myQueueResponse = await processor.GetAsync("/api/v1/queues/me?page=1&pageSize=5");
        var myQueue = await ReadSuccessAsync<PagedResult<QueueItemResponse>>(myQueueResponse);
        var teamQueueResponse = await teamLead.GetAsync("/api/v1/queues/team?page=1&pageSize=2");
        var teamQueue = await ReadSuccessAsync<PagedResult<QueueItemResponse>>(teamQueueResponse);

        Assert.Equal(HttpStatusCode.OK, myQueueResponse.StatusCode);
        Assert.Contains(myQueue.Items, item => item.LoanId == assignedLoanId);
        Assert.Equal(HttpStatusCode.OK, teamQueueResponse.StatusCode);
        Assert.True(teamQueue.TotalCount >= 2);
        Assert.Equal(2, teamQueue.Items.Count);
        Assert.True(teamQueue.Items.First().PriorityScore >= teamQueue.Items.Last().PriorityScore);
    }

    [Fact]
    public async Task RoundRobinTieBreaker_RotatesAndPersistsAcrossContexts()
    {
        var routingKey = $"test:{Guid.NewGuid():N}";
        var first = Candidate("Alpha");
        var second = Candidate("Beta");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
            var tieBreaker = new EfRoundRobinTieBreaker(dbContext);
            var selected = await tieBreaker.SelectAsync(routingKey, [first, second], CancellationToken.None);
            await dbContext.SaveChangesAsync();
            Assert.Equal(first.EmployeeId, selected.EmployeeId);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
            var tieBreaker = new EfRoundRobinTieBreaker(dbContext);
            var selected = await tieBreaker.SelectAsync(routingKey, [first, second], CancellationToken.None);
            await dbContext.SaveChangesAsync();
            Assert.Equal(second.EmployeeId, selected.EmployeeId);
        }
    }

    [Fact]
    public async Task ConcurrentAutomaticAssignment_ProducesOneFinalAssignee()
    {
        var teamLeadA = await CreateAuthorizedClientAsync("teamlead@example.test");
        var teamLeadB = await CreateAuthorizedClientAsync("teamlead@example.test");
        var loanId = await InsertLoanAsync(LoanStatus.Submitted, assigneeId: null);

        var responses = await Task.WhenAll(
            teamLeadA.PostAsync($"/api/v1/loans/{loanId}/assign", content: null),
            teamLeadB.PostAsync($"/api/v1/loans/{loanId}/assign", content: null));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var loan = await dbContext.LoanApplications.SingleAsync(item => item.Id == loanId);
        var assignmentCount = await dbContext.LoanAssignments.CountAsync(item => item.LoanApplicationId == loanId);

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.NotNull(loan.AssigneeId);
        Assert.Equal(1, assignmentCount);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = MortgageFlowApiFactory.DemoPassword });
        var login = await ReadSuccessAsync<LoginResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private async Task<Guid> FindLoanIdAsync(string loanNumber)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var number = LoanNumber.Create(loanNumber);
        return await dbContext.LoanApplications
            .Where(loan => loan.LoanNumber == number)
            .Select(loan => loan.Id)
            .SingleAsync();
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

    private async Task<Guid> InsertLoanAsync(LoanStatus status, Guid? assigneeId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var broker = await dbContext.Users.SingleAsync(user => user.Email == "broker@example.test");
        var processor = await dbContext.Users.SingleAsync(user => user.Email == "processor@example.test");
        var underwriter = await dbContext.Users.SingleAsync(user => user.Email == "underwriter@example.test");
        var loan = CreateCompleteDomainLoan(broker.Id, DateTime.UtcNow.AddMinutes(-45));

        loan.TransitionTo(LoanStatus.Submitted, broker.Id, null, loan.UpdatedUtc.AddMinutes(1));
        if (status == LoanStatus.Processing)
        {
            loan.TransitionTo(LoanStatus.Processing, processor.Id, null, loan.UpdatedUtc.AddMinutes(1));
        }
        else if (status == LoanStatus.Underwriting)
        {
            loan.TransitionTo(LoanStatus.Processing, processor.Id, null, loan.UpdatedUtc.AddMinutes(1));
            loan.TransitionTo(LoanStatus.Underwriting, processor.Id, "Synthetic checklist complete.", loan.UpdatedUtc.AddMinutes(1));
        }

        if (assigneeId is not null)
        {
            loan.AssignTo(assigneeId.Value, loan.UpdatedUtc.AddMinutes(1));
        }
        else if (status == LoanStatus.Underwriting)
        {
            _ = underwriter;
        }

        dbContext.LoanApplications.Add(loan);
        await dbContext.SaveChangesAsync();
        return loan.Id;
    }

    private static LoanApplication CreateCompleteDomainLoan(Guid brokerId, DateTime createdUtc)
    {
        var sequence = Interlocked.Increment(ref _loanNumberSequence);
        var loan = LoanApplication.CreateDraft(
            LoanNumber.Create($"MF-{sequence:000000}"),
            brokerId,
            Money.Usd(245_000),
            createdUtc);
        loan.AddBorrower(
            Borrower.Create($"Assignment Borrower {sequence}", $"assignment{sequence}@example.test", Money.Usd(125_000)),
            createdUtc.AddMinutes(1));
        loan.AddProperty(
            Property.Create(
                "123 Assignment Street",
                "Pontiac",
                "MI",
                "48341",
                Money.Usd(360_000),
                OccupancyType.PrimaryResidence),
            createdUtc.AddMinutes(2));
        loan.UpdateLoanTerms(Money.Usd(245_000), LoanPurpose.Purchase, 6.5m, 360, createdUtc.AddMinutes(3));

        return loan;
    }

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Expected response body was empty.");
    }

    private static EmployeeAssignmentCandidate Candidate(string name)
    {
        return new EmployeeAssignmentCandidate(
            Guid.NewGuid(),
            name,
            "Processor",
            Guid.NewGuid(),
            IsActive: true,
            IsAvailable: true,
            CapacityPoints: 8,
            SkillTags: ["processing"],
            OpenTaskWeight: 0,
            ActiveLoanCount: 0);
    }

    private sealed record LoginResponse(string AccessToken);
}

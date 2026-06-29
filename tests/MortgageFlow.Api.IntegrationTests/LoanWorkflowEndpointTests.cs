using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MortgageFlow.Application.Loans;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Persistence;
using MortgageFlow.Infrastructure.Persistence.Entities;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class LoanWorkflowEndpointTests : IClassFixture<MortgageFlowApiFactory>
{
    private static int _loanNumberSequence = 700000;

    private readonly MortgageFlowApiFactory _factory;

    public LoanWorkflowEndpointTests(MortgageFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Broker_CreatesDraftAndUpdatesOnlyOwnedDraft()
    {
        var broker = await CreateAuthorizedClientAsync("broker@example.test");
        var loan = await CreateDraftAsync(broker, "Owned Draft");

        var updateResponse = await broker.PutAsJsonAsync($"/api/v1/loans/{loan.Id}", CompleteUpdate(loan.RowVersion, "Updated Draft"));
        var updated = await ReadSuccessAsync<LoanDetailResponse>(updateResponse);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Updated Draft", updated.Borrower?.FullName);

        var processor = await CreateAuthorizedClientAsync("processor@example.test");
        var hiddenResponse = await processor.PutAsJsonAsync($"/api/v1/loans/{loan.Id}", CompleteUpdate(updated.RowVersion, "Hidden"));

        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
    }

    [Fact]
    public async Task IncompleteSubmission_ReturnsValidationProblemAndRemainsDraft()
    {
        var broker = await CreateAuthorizedClientAsync("broker@example.test");
        var loan = await CreateDraftAsync(broker, "Incomplete Draft", complete: false);

        var response = await broker.PostAsJsonAsync(
            $"/api/v1/loans/{loan.Id}/transitions",
            new TransitionLoanRequest(LoanStatus.Submitted, null, loan.RowVersion));

        var refreshed = await ReadSuccessAsync<LoanDetailResponse>(await broker.GetAsync($"/api/v1/loans/{loan.Id}"));
        var history = await ReadSuccessAsync<IReadOnlyCollection<LoanStatusHistoryResponse>>(
            await broker.GetAsync($"/api/v1/loans/{loan.Id}/history"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(LoanStatus.Draft, refreshed.Status);
        Assert.Empty(history);
    }

    [Fact]
    public async Task CompleteSubmission_WritesSubmittedStatusHistoryAndAudit()
    {
        var broker = await CreateAuthorizedClientAsync("broker@example.test");
        var loan = await CreateDraftAsync(broker, "Complete Submit");

        var response = await broker.PostAsJsonAsync(
            $"/api/v1/loans/{loan.Id}/transitions",
            new TransitionLoanRequest(LoanStatus.Submitted, null, loan.RowVersion));
        var submitted = await ReadSuccessAsync<LoanDetailResponse>(response);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var historyCount = await dbContext.LoanApplications
            .Where(item => item.Id == loan.Id)
            .SelectMany(item => item.StatusHistory)
            .CountAsync();
        var auditCount = await dbContext.AuditLogs.CountAsync(item =>
            item.EntityId == loan.Id.ToString() && item.Action == "LoanTransitioned");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(LoanStatus.Submitted, submitted.Status);
        Assert.Equal(1, historyCount);
        Assert.Equal(1, auditCount);
    }

    [Fact]
    public async Task Broker_CannotTransitionToProcessing()
    {
        var broker = await CreateAuthorizedClientAsync("broker@example.test");
        var loan = await CreateDraftAsync(broker, "Wrong Role Processing");

        var response = await broker.PostAsJsonAsync(
            $"/api/v1/loans/{loan.Id}/transitions",
            new TransitionLoanRequest(LoanStatus.Processing, null, loan.RowVersion));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Processor_CannotSkipDirectlyToApproved()
    {
        var loanId = await InsertAssignedLoanAsync(LoanStatus.Submitted, "processor@example.test");
        var processor = await CreateAuthorizedClientAsync("processor@example.test");
        var loan = await ReadSuccessAsync<LoanDetailResponse>(await processor.GetAsync($"/api/v1/loans/{loanId}"));

        var response = await processor.PostAsJsonAsync(
            $"/api/v1/loans/{loanId}/transitions",
            new TransitionLoanRequest(LoanStatus.Approved, "Cannot skip.", loan.RowVersion));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Underwriter_CanApproveAssignedUnderwritingLoan()
    {
        var loanId = await InsertAssignedLoanAsync(LoanStatus.Underwriting, "underwriter@example.test");
        var underwriter = await CreateAuthorizedClientAsync("underwriter@example.test");
        var loan = await ReadSuccessAsync<LoanDetailResponse>(await underwriter.GetAsync($"/api/v1/loans/{loanId}"));

        var response = await underwriter.PostAsJsonAsync(
            $"/api/v1/loans/{loanId}/transitions",
            new TransitionLoanRequest(LoanStatus.Approved, "Synthetic underwriting approved.", loan.RowVersion));
        var approved = await ReadSuccessAsync<LoanDetailResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(LoanStatus.Approved, approved.Status);
    }

    [Fact]
    public async Task InvalidTransition_WritesNoHistoryOrAudit()
    {
        var loanId = await InsertAssignedLoanAsync(LoanStatus.Processing, "processor@example.test");
        var processor = await CreateAuthorizedClientAsync("processor@example.test");
        var loan = await ReadSuccessAsync<LoanDetailResponse>(await processor.GetAsync($"/api/v1/loans/{loanId}"));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var beforeHistory = await CountHistoryAsync(dbContext, loanId);
        var beforeAudit = await CountAuditAsync(dbContext, loanId);

        var response = await processor.PostAsJsonAsync(
            $"/api/v1/loans/{loanId}/transitions",
            new TransitionLoanRequest(LoanStatus.Underwriting, "", loan.RowVersion));

        var afterHistory = await CountHistoryAsync(dbContext, loanId);
        var afterAudit = await CountAuditAsync(dbContext, loanId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(beforeHistory, afterHistory);
        Assert.Equal(beforeAudit, afterAudit);
    }

    [Fact]
    public async Task UnauthorizedVisibilityAndWrongRoleUseConsistentStatuses()
    {
        var broker = await CreateAuthorizedClientAsync("broker@example.test");
        var loan = await CreateDraftAsync(broker, "Hidden Loan");

        var processor = await CreateAuthorizedClientAsync("processor@example.test");
        var hiddenResponse = await processor.GetAsync($"/api/v1/loans/{loan.Id}");

        var teamLead = await CreateAuthorizedClientAsync("teamlead@example.test");
        var visible = await ReadSuccessAsync<LoanDetailResponse>(await teamLead.GetAsync($"/api/v1/loans/{loan.Id}"));
        var forbiddenResponse = await teamLead.PostAsJsonAsync(
            $"/api/v1/loans/{loan.Id}/transitions",
            new TransitionLoanRequest(LoanStatus.Submitted, null, visible.RowVersion));

        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task ConcurrentUpdates_WithSameRowVersion_ReturnOneConflict()
    {
        var broker = await CreateAuthorizedClientAsync("broker@example.test");
        var loan = await CreateDraftAsync(broker, "Concurrent Original");
        var first = CompleteUpdate(loan.RowVersion, "Concurrent First");
        var second = CompleteUpdate(loan.RowVersion, "Concurrent Second");

        var firstResponse = await broker.PutAsJsonAsync($"/api/v1/loans/{loan.Id}", first);
        var secondResponse = await broker.PutAsJsonAsync($"/api/v1/loans/{loan.Id}", second);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task List_UsesPaginationFilterAndDeterministicSorting()
    {
        var broker = await CreateAuthorizedClientAsync("broker@example.test");
        var marker = $"SortMarker{Guid.NewGuid():N}";

        await CreateDraftAsync(broker, $"{marker} A");
        await CreateDraftAsync(broker, $"{marker} B");
        await CreateDraftAsync(broker, $"{marker} C");

        var response = await broker.GetAsync($"/api/v1/loans?page=1&pageSize=2&search={marker}&status=Draft");
        var page = await ReadSuccessAsync<PagedResult<LoanListItemResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal($"{marker} C", page.Items.First().BorrowerName);
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

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Expected response body was empty.");
    }

    private async Task<LoanDetailResponse> CreateDraftAsync(HttpClient client, string borrowerName, bool complete = true)
    {
        var request = complete
            ? CompleteCreate(borrowerName)
            : new CreateLoanRequest(250_000, null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/v1/loans", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadSuccessAsync<LoanDetailResponse>(response);
    }

    private static CreateLoanRequest CompleteCreate(string borrowerName)
    {
        return new CreateLoanRequest(
            250_000,
            LoanPurpose.Purchase,
            6.75m,
            360,
            new BorrowerDto(borrowerName, $"{Guid.NewGuid():N}@example.test", 125_000),
            new PropertyDto(
                "123 Synthetic Street",
                "Birmingham",
                "MI",
                "48009",
                350_000,
                OccupancyType.PrimaryResidence));
    }

    private static UpdateLoanRequest CompleteUpdate(string rowVersion, string borrowerName)
    {
        return new UpdateLoanRequest(
            rowVersion,
            255_000,
            LoanPurpose.Purchase,
            6.5m,
            360,
            new BorrowerDto(borrowerName, $"{Guid.NewGuid():N}@example.test", 130_000),
            new PropertyDto(
                "456 Updated Street",
                "Birmingham",
                "MI",
                "48009",
                360_000,
                OccupancyType.PrimaryResidence));
    }

    private async Task<Guid> InsertAssignedLoanAsync(LoanStatus targetStatus, string assigneeEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var broker = await dbContext.Users.SingleAsync(user => user.Email == "broker@example.test");
        var assignee = await dbContext.Users.SingleAsync(user => user.Email == assigneeEmail);
        var processor = await dbContext.Users.SingleAsync(user => user.Email == "processor@example.test");
        var now = DateTime.UtcNow.AddMinutes(-30);
        var loan = CreateCompleteDomainLoan(broker.Id, now);

        loan.TransitionTo(LoanStatus.Submitted, broker.Id, null, now.AddMinutes(4));

        if (targetStatus == LoanStatus.Submitted)
        {
            loan.AssignTo(assignee.Id, now.AddMinutes(5));
        }
        else if (targetStatus == LoanStatus.Processing)
        {
            loan.AssignTo(assignee.Id, now.AddMinutes(5));
            loan.TransitionTo(LoanStatus.Processing, assignee.Id, null, now.AddMinutes(6));
        }
        else if (targetStatus == LoanStatus.Underwriting)
        {
            loan.AssignTo(processor.Id, now.AddMinutes(5));
            loan.TransitionTo(LoanStatus.Processing, processor.Id, null, now.AddMinutes(6));
            loan.TransitionTo(LoanStatus.Underwriting, processor.Id, "Synthetic checklist complete.", now.AddMinutes(7));
            loan.AssignTo(assignee.Id, now.AddMinutes(8));
        }
        else
        {
            throw new InvalidOperationException($"Unsupported seeded status {targetStatus}.");
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
            Money.Usd(250_000),
            createdUtc);
        loan.AddBorrower(
            Borrower.Create($"Assigned Borrower {sequence}", $"assigned{sequence}@example.test", Money.Usd(125_000)),
            createdUtc.AddMinutes(1));
        loan.AddProperty(
            Property.Create(
                "789 Assigned Street",
                "Birmingham",
                "MI",
                "48009",
                Money.Usd(350_000),
                OccupancyType.PrimaryResidence),
            createdUtc.AddMinutes(2));
        loan.UpdateLoanTerms(Money.Usd(250_000), LoanPurpose.Purchase, 6.75m, 360, createdUtc.AddMinutes(3));

        return loan;
    }

    private static async Task<int> CountHistoryAsync(MortgageFlowDbContext dbContext, Guid loanId)
    {
        return await dbContext.LoanApplications
            .Where(item => item.Id == loanId)
            .SelectMany(item => item.StatusHistory)
            .CountAsync();
    }

    private static async Task<int> CountAuditAsync(MortgageFlowDbContext dbContext, Guid loanId)
    {
        return await dbContext.AuditLogs.CountAsync(item => item.EntityId == loanId.ToString());
    }

    private sealed record LoginResponse(string AccessToken);
}

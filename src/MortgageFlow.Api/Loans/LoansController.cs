using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Application.Loans;
using MortgageFlow.Domain;

namespace MortgageFlow.Api.Loans;

[ApiController]
[Authorize]
[Route("api/v1/loans")]
public sealed class LoansController : ControllerBase
{
    private readonly ILoanAssignmentService _loanAssignmentService;
    private readonly ILoanWorkflowService _loanWorkflowService;

    public LoansController(
        ILoanAssignmentService loanAssignmentService,
        ILoanWorkflowService loanWorkflowService)
    {
        _loanAssignmentService = loanAssignmentService;
        _loanWorkflowService = loanWorkflowService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        var result = await _loanWorkflowService.CreateDraftAsync(request, cancellationToken);
        return ToActionResult(result, createdRouteName: nameof(GetById));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] LoanStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _loanWorkflowService.ListAsync(page, pageSize, search, status, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _loanWorkflowService.GetDetailAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateLoanRequest request, CancellationToken cancellationToken)
    {
        var result = await _loanWorkflowService.UpdateDraftAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/transitions")]
    public async Task<IActionResult> Transition(
        Guid id,
        TransitionLoanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loanWorkflowService.TransitionAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken)
    {
        var result = await _loanWorkflowService.GetHistoryAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, CancellationToken cancellationToken)
    {
        var result = await _loanAssignmentService.AssignAutomaticallyAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/reassign")]
    public async Task<IActionResult> Reassign(
        Guid id,
        ManualReassignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loanAssignmentService.ReassignAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/priority")]
    public async Task<IActionResult> UpdatePriority(
        Guid id,
        PriorityUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loanAssignmentService.UpdatePriorityAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(LoanActionResult<T> result, string? createdRouteName = null)
    {
        return result.Status switch
        {
            LoanActionStatus.Success when createdRouteName is not null && result.Value is LoanDetailResponse loan =>
                CreatedAtRoute(createdRouteName, new { id = loan.Id }, result.Value),
            LoanActionStatus.Success => Ok(result.Value),
            LoanActionStatus.NotFound => NotFound(),
            LoanActionStatus.Forbidden => Forbid(),
            LoanActionStatus.Conflict => Conflict(new ProblemDetails
            {
                Title = "Concurrency conflict.",
                Detail = result.Message,
                Status = StatusCodes.Status409Conflict
            }),
            LoanActionStatus.InvalidTransition => BadRequest(new ProblemDetails
            {
                Title = "Invalid loan transition.",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest
            }),
            LoanActionStatus.ValidationFailed => ValidationProblem(new ValidationProblemDetails(
                result.ValidationErrors.ToDictionary(item => item.Key, item => item.Value))
            {
                Title = "Loan validation failed.",
                Status = StatusCodes.Status400BadRequest
            }),
            _ => Problem("Unexpected loan workflow result.", statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}

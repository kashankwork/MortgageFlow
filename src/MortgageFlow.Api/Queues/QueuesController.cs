using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Application.Loans;
using MortgageFlow.Domain;

namespace MortgageFlow.Api.Queues;

[ApiController]
[Authorize]
[Route("api/v1/queues")]
public sealed class QueuesController : ControllerBase
{
    private readonly IQueueService _queueService;

    public QueuesController(IQueueService queueService)
    {
        _queueService = queueService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> MyQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] LoanStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _queueService.GetMyQueueAsync(page, pageSize, status, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("team")]
    public async Task<IActionResult> TeamQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] LoanStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _queueService.GetTeamQueueAsync(page, pageSize, status, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(LoanActionResult<T> result)
    {
        return result.Status switch
        {
            LoanActionStatus.Success => Ok(result.Value),
            LoanActionStatus.NotFound => NotFound(),
            LoanActionStatus.Forbidden => Forbid(),
            LoanActionStatus.Conflict => Conflict(new ProblemDetails
            {
                Title = "Concurrency conflict.",
                Detail = result.Message,
                Status = StatusCodes.Status409Conflict
            }),
            LoanActionStatus.ValidationFailed => ValidationProblem(new ValidationProblemDetails(
                result.ValidationErrors.ToDictionary(item => item.Key, item => item.Value))
            {
                Title = "Queue validation failed.",
                Status = StatusCodes.Status400BadRequest
            }),
            _ => Problem("Unexpected queue result.", statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}

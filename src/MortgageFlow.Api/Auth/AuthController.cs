using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Application.Users;

namespace MortgageFlow.Api.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthenticationService authenticationService, ICurrentUser currentUser)
    {
        _authenticationService = authenticationService;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            // Keep both missing users and bad passwords indistinguishable to avoid account enumeration.
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed.",
                Detail = "The supplied credentials are invalid.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(result.User);
    }

    /// <summary>
    /// Returns the authenticated caller decoded from JWT claims; useful for client bootstrapping.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return _currentUser.User is null ? Unauthorized() : Ok(_currentUser.User);
    }

    /// <summary>
    /// Small policy probe used by integration tests until product endpoints require Team Lead access.
    /// </summary>
    [HttpGet("team-lead-check")]
    [Authorize(Policy = AuthorizationPolicies.TeamLead)]
    public IActionResult TeamLeadCheck()
    {
        return Ok(new { authorized = true });
    }
}

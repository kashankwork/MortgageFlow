using System.ComponentModel.DataAnnotations;

namespace MortgageFlow.Api.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

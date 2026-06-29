using System.Diagnostics;
using System.Security.Claims;

namespace MortgageFlow.Api.Diagnostics;

public sealed class CorrelationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationLoggingMiddleware> _logger;

    public CorrelationLoggingMiddleware(RequestDelegate next, ILogger<CorrelationLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[HttpCorrelationContext.ItemName] = correlationId;
        context.Response.Headers[HttpCorrelationContext.HeaderName] = correlationId;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms. CorrelationId={CorrelationId} UserId={UserId} LoanId={LoanId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                context.Request.RouteValues.TryGetValue("id", out var loanId) ? loanId : null);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var provided = context.Request.Headers[HttpCorrelationContext.HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(provided) && provided.Length <= 120 && IsSafeCorrelationId(provided))
        {
            return provided.Trim();
        }

        return context.TraceIdentifier;
    }

    private static bool IsSafeCorrelationId(string value)
    {
        return value.All(character =>
            char.IsLetterOrDigit(character) ||
            character is '-' or '_' or '.' or ':' or '/');
    }
}

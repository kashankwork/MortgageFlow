using MortgageFlow.Application.Diagnostics;

namespace MortgageFlow.Api.Diagnostics;

public sealed class HttpCorrelationContext : ICorrelationContext
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemName = "MortgageFlow.CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(ItemName, out var value) == true && value is string correlationId)
            {
                return correlationId;
            }

            return httpContext?.TraceIdentifier ?? string.Empty;
        }
    }
}

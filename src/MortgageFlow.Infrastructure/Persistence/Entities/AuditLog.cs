namespace MortgageFlow.Infrastructure.Persistence.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }

    public Guid? ActorId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}

namespace MortgageFlow.Infrastructure.Persistence.Entities;

public sealed class RoundRobinCursor
{
    public Guid Id { get; set; }

    public string RoutingKey { get; set; } = string.Empty;

    public Guid? LastSelectedEmployeeId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

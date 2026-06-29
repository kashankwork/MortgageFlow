namespace MortgageFlow.Infrastructure.Persistence.Entities;

public sealed class LoanAssignmentRecord
{
    public Guid Id { get; set; }

    public Guid LoanApplicationId { get; set; }

    public Guid? PreviousAssigneeId { get; set; }

    public Guid NewAssigneeId { get; set; }

    public bool IsAutomatic { get; set; }

    public Guid ActorId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime AssignedUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

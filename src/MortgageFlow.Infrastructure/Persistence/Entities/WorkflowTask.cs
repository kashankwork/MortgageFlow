using MortgageFlow.Domain;

namespace MortgageFlow.Infrastructure.Persistence.Entities;

public sealed class WorkflowTask
{
    public Guid Id { get; set; }

    public Guid LoanApplicationId { get; set; }

    public Guid AssigneeId { get; set; }

    public BusinessPriority Priority { get; set; }

    public DateTime DueUtc { get; set; }

    public bool IsComplete { get; set; }
}

using Microsoft.AspNetCore.Identity;

namespace MortgageFlow.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public Guid TeamId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsAvailable { get; set; } = true;

    public int CapacityPoints { get; set; } = 8;
}

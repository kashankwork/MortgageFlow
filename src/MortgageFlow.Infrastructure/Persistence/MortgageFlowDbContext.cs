using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Identity;
using MortgageFlow.Infrastructure.Persistence.Entities;

namespace MortgageFlow.Infrastructure.Persistence;

public sealed class MortgageFlowDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public MortgageFlowDbContext(DbContextOptions<MortgageFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();

    public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();

    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();

    public DbSet<LoanAssignmentRecord> LoanAssignments => Set<LoanAssignmentRecord>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RoundRobinCursor> RoundRobinCursors => Set<RoundRobinCursor>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureLoanApplication(builder);
        ConfigureSupportingEntities(builder);
    }

    private static void ConfigureLoanApplication(ModelBuilder builder)
    {
        builder.Entity<LoanApplication>(loan =>
        {
            loan.ToTable("LoanApplications");
            loan.HasKey(x => x.Id);

            // Value objects stay in Domain; EF converts them to scalar columns at the boundary.
            loan.Property(x => x.LoanNumber)
                .HasConversion(x => x.Value, x => LoanNumber.Create(x))
                .HasMaxLength(9)
                .IsRequired();
            loan.HasIndex(x => x.LoanNumber).IsUnique();

            loan.Property(x => x.BrokerId).IsRequired();
            loan.Property(x => x.AssigneeId);
            loan.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            loan.Property(x => x.BusinessPriority).HasConversion<string>().HasMaxLength(20).IsRequired();
            loan.Property(x => x.CreatedUtc).IsRequired();
            loan.Property(x => x.UpdatedUtc).IsRequired();
            loan.Property(x => x.SubmittedUtc);

            // Owned types keep the aggregate cohesive while still producing queryable SQL columns.
            loan.OwnsOne(x => x.RequestedAmount, money =>
            {
                money.Property(x => x.Amount).HasColumnName("RequestedAmount").HasColumnType("decimal(18,2)");
                money.Property(x => x.Currency).HasColumnName("RequestedAmountCurrency").HasMaxLength(3);
            });

            loan.OwnsOne(x => x.Borrower, borrower =>
            {
                borrower.Property(x => x.FullName).HasMaxLength(160);
                borrower.Property(x => x.Email).HasMaxLength(256);
                borrower.OwnsOne(x => x.AnnualIncome, money =>
                {
                    money.Property(x => x.Amount).HasColumnName("BorrowerAnnualIncome").HasColumnType("decimal(18,2)");
                    money.Property(x => x.Currency).HasColumnName("BorrowerAnnualIncomeCurrency").HasMaxLength(3);
                });
            });

            loan.OwnsOne(x => x.Property, property =>
            {
                property.Property(x => x.StreetAddress).HasMaxLength(240);
                property.Property(x => x.City).HasMaxLength(120);
                property.Property(x => x.State).HasMaxLength(2);
                property.Property(x => x.PostalCode).HasMaxLength(20);
                property.OwnsOne(x => x.EstimatedValue, money =>
                {
                    money.Property(x => x.Amount).HasColumnName("PropertyEstimatedValue").HasColumnType("decimal(18,2)");
                    money.Property(x => x.Currency).HasColumnName("PropertyEstimatedValueCurrency").HasMaxLength(3);
                });
            });

            loan.OwnsMany(
                x => x.StatusHistory,
                history =>
                {
                    // Status history belongs to the loan aggregate and is persisted atomically with it.
                    history.ToTable("LoanStatusHistory");
                    history.WithOwner().HasForeignKey("LoanApplicationId");
                    history.Property<int>("Id");
                    history.HasKey("Id");
                    history.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
                    history.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
                    history.Property(x => x.ActorId).IsRequired();
                    history.Property(x => x.Reason).HasMaxLength(500);
                    history.Property(x => x.ChangedUtc).IsRequired();
                    history.HasIndex("LoanApplicationId", nameof(LoanStatusChange.ChangedUtc));
                });

            // Row-version protects future edit/transition endpoints from silent overwrite.
            loan.Property<byte[]>("RowVersion").IsRowVersion();
        });
    }

    private static void ConfigureSupportingEntities(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            user.Property(x => x.TeamId).IsRequired();
            user.Property(x => x.IsActive).IsRequired();
            user.Property(x => x.IsAvailable).IsRequired();
            user.Property(x => x.CapacityPoints).IsRequired();
        });

        builder.Entity<EmployeeSkill>(skill =>
        {
            skill.ToTable("EmployeeSkills");
            skill.HasKey(x => x.Id);
            skill.Property(x => x.SkillTag).HasMaxLength(80).IsRequired();
            // One skill tag per employee prevents duplicate eligibility signals.
            skill.HasIndex(x => new { x.UserId, x.SkillTag }).IsUnique();
        });

        builder.Entity<WorkflowTask>(task =>
        {
            task.ToTable("WorkflowTasks");
            task.HasKey(x => x.Id);
            task.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            task.HasIndex(x => new { x.AssigneeId, x.IsComplete, x.DueUtc });
        });

        builder.Entity<LoanAssignmentRecord>(assignment =>
        {
            assignment.ToTable("LoanAssignments");
            assignment.HasKey(x => x.Id);
            assignment.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            assignment.Property(x => x.RowVersion).IsRowVersion();
            assignment.HasIndex(x => new { x.LoanApplicationId, x.AssignedUtc });
        });

        builder.Entity<AuditLog>(audit =>
        {
            audit.ToTable("AuditLogs");
            audit.HasKey(x => x.Id);
            audit.Property(x => x.Action).HasMaxLength(120).IsRequired();
            audit.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
            audit.Property(x => x.EntityId).HasMaxLength(80).IsRequired();
            audit.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            audit.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            audit.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedUtc });
        });

        builder.Entity<RoundRobinCursor>(cursor =>
        {
            cursor.ToTable("RoundRobinCursors");
            cursor.HasKey(x => x.Id);
            cursor.Property(x => x.RoutingKey).HasMaxLength(160).IsRequired();
            cursor.Property(x => x.RowVersion).IsRowVersion();
            // The assignment engine will use one cursor per routing lane for deterministic tie-breaking.
            cursor.HasIndex(x => x.RoutingKey).IsUnique();
        });
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MortgageFlow.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                CapacityPoints = table.Column<int>(type: "int", nullable: false),
                UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                AccessFailedCount = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                EntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EmployeeSkills",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SkillTag = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeSkills", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "LoanApplications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LoanNumber = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                BusinessPriority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                RequestedAmountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                Borrower_FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                Borrower_Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                BorrowerAnnualIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                BorrowerAnnualIncomeCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                Property_StreetAddress = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                Property_City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                Property_State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                Property_PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                PropertyEstimatedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                PropertyEstimatedValueCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                SubmittedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoanApplications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "LoanAssignments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PreviousAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                NewAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                AssignedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoanAssignments", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RoundRobinCursors",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoutingKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                LastSelectedEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoundRobinCursors", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WorkflowTasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                DueUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsComplete = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkflowTasks", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LoanStatusHistory",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PreviousStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                NewStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ChangedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoanStatusHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_LoanStatusHistory_LoanApplications_LoanApplicationId",
                    column: x => x.LoanApplicationId,
                    principalTable: "LoanApplications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AspNetRoleClaims_RoleId",
            table: "AspNetRoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "NormalizedName",
            unique: true,
            filter: "[NormalizedName] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserClaims_UserId",
            table: "AspNetUserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserLogins_UserId",
            table: "AspNetUserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserRoles_RoleId",
            table: "AspNetUserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true,
            filter: "[NormalizedUserName] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_EntityType_EntityId_CreatedUtc",
            table: "AuditLogs",
            columns: new[] { "EntityType", "EntityId", "CreatedUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeSkills_UserId_SkillTag",
            table: "EmployeeSkills",
            columns: new[] { "UserId", "SkillTag" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LoanApplications_LoanNumber",
            table: "LoanApplications",
            column: "LoanNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LoanAssignments_LoanApplicationId_AssignedUtc",
            table: "LoanAssignments",
            columns: new[] { "LoanApplicationId", "AssignedUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_LoanStatusHistory_LoanApplicationId_ChangedUtc",
            table: "LoanStatusHistory",
            columns: new[] { "LoanApplicationId", "ChangedUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_RoundRobinCursors_RoutingKey",
            table: "RoundRobinCursors",
            column: "RoutingKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WorkflowTasks_AssigneeId_IsComplete_DueUtc",
            table: "WorkflowTasks",
            columns: new[] { "AssigneeId", "IsComplete", "DueUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AspNetRoleClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserLogins");

        migrationBuilder.DropTable(
            name: "AspNetUserRoles");

        migrationBuilder.DropTable(
            name: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "AuditLogs");

        migrationBuilder.DropTable(
            name: "EmployeeSkills");

        migrationBuilder.DropTable(
            name: "LoanAssignments");

        migrationBuilder.DropTable(
            name: "LoanStatusHistory");

        migrationBuilder.DropTable(
            name: "RoundRobinCursors");

        migrationBuilder.DropTable(
            name: "WorkflowTasks");

        migrationBuilder.DropTable(
            name: "AspNetRoles");

        migrationBuilder.DropTable(
            name: "AspNetUsers");

        migrationBuilder.DropTable(
            name: "LoanApplications");
    }
}

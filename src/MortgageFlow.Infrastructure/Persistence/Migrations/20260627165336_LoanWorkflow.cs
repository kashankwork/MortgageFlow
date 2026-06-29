using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MortgageFlow.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class LoanWorkflow : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "InterestRatePercent",
            table: "LoanApplications",
            type: "decimal(5,3)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LoanPurpose",
            table: "LoanApplications",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Property_OccupancyType",
            table: "LoanApplications",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "TermMonths",
            table: "LoanApplications",
            type: "int",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "InterestRatePercent",
            table: "LoanApplications");

        migrationBuilder.DropColumn(
            name: "LoanPurpose",
            table: "LoanApplications");

        migrationBuilder.DropColumn(
            name: "Property_OccupancyType",
            table: "LoanApplications");

        migrationBuilder.DropColumn(
            name: "TermMonths",
            table: "LoanApplications");
    }
}

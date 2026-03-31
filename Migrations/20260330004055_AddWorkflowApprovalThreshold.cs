using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowApprovalThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalThresholdAmount",
                table: "Workflows",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalThresholdAmount",
                table: "Workflows");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGlAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlAccounts",
                columns: table => new
                {
                    GlAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlAccounts", x => x.GlAccountId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlAccounts_TenantId_Code",
                table: "GlAccounts",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlAccounts_TenantId_GlAccountId",
                table: "GlAccounts",
                columns: new[] { "TenantId", "GlAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_GlAccounts_TenantId_IsActive",
                table: "GlAccounts",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_GlAccounts_TenantId_Name",
                table: "GlAccounts",
                columns: new[] { "TenantId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlAccounts");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentsBusinessUnitsAndUserOrg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusinessUnitId",
                table: "TenantUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "TenantUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUnits",
                columns: table => new
                {
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUnits", x => x.BusinessUnitId);
                    table.ForeignKey(
                        name: "FK_BusinessUnits_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_BusinessUnitId",
                table: "TenantUsers",
                column: "BusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_DepartmentId",
                table: "TenantUsers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_TenantId_BusinessUnitId",
                table: "TenantUsers",
                columns: new[] { "TenantId", "BusinessUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_TenantId_DepartmentId",
                table: "TenantUsers",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_DepartmentId",
                table: "BusinessUnits",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_TenantId_BusinessUnitId",
                table: "BusinessUnits",
                columns: new[] { "TenantId", "BusinessUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_TenantId_DepartmentId_Name",
                table: "BusinessUnits",
                columns: new[] { "TenantId", "DepartmentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_TenantId_IsActive",
                table: "BusinessUnits",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_DepartmentId",
                table: "Departments",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_IsActive",
                table: "Departments",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_Name",
                table: "Departments",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_BusinessUnits_BusinessUnitId",
                table: "TenantUsers",
                column: "BusinessUnitId",
                principalTable: "BusinessUnits",
                principalColumn: "BusinessUnitId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_Departments_DepartmentId",
                table: "TenantUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_BusinessUnits_BusinessUnitId",
                table: "TenantUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_Departments_DepartmentId",
                table: "TenantUsers");

            migrationBuilder.DropTable(
                name: "BusinessUnits");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_BusinessUnitId",
                table: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_DepartmentId",
                table: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_TenantId_BusinessUnitId",
                table: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_TenantId_DepartmentId",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "BusinessUnitId",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "TenantUsers");
        }
    }
}

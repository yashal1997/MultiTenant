using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantUserDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_Departments_DepartmentId",
                table: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_DepartmentId",
                table: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_TenantId_DepartmentId",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "TenantUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "TenantUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_DepartmentId",
                table: "TenantUsers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_TenantId_DepartmentId",
                table: "TenantUsers",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_Departments_DepartmentId",
                table: "TenantUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

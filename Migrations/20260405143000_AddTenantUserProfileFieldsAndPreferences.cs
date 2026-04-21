using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MultiTenant.Api.Infrastructure.Persistence;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260405143000_AddTenantUserProfileFieldsAndPreferences")]
    public partial class AddTenantUserProfileFieldsAndPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "TenantUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LineManagerUserId",
                table: "TenantUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "NotificationSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PushNotificationsEnabled",
                table: "NotificationSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_LineManagerUserId",
                table: "TenantUsers",
                column: "LineManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_AspNetUsers_LineManagerUserId",
                table: "TenantUsers",
                column: "LineManagerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_AspNetUsers_LineManagerUserId",
                table: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_LineManagerUserId",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "LineManagerUserId",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                table: "NotificationSettings");

            migrationBuilder.DropColumn(
                name: "PushNotificationsEnabled",
                table: "NotificationSettings");
        }
    }
}

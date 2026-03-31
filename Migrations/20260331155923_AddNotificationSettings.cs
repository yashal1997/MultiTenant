using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    NotificationSettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailExpenseSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    EmailExpenseApproved = table.Column<bool>(type: "bit", nullable: false),
                    EmailExpenseRejected = table.Column<bool>(type: "bit", nullable: false),
                    EmailPendingApprovalsDigest = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.NotificationSettingId);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_TenantId_UserId",
                table: "NotificationSettings",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_UserId",
                table: "NotificationSettings",
                column: "UserId");

            migrationBuilder.Sql("""
                INSERT INTO [NotificationSettings]
                    ([NotificationSettingId], [TenantId], [UserId], [EmailExpenseSubmitted], [EmailExpenseApproved], [EmailExpenseRejected], [EmailPendingApprovalsDigest], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT
                    NEWID(),
                    tu.[TenantId],
                    tu.[UserId],
                    CAST(1 AS bit),
                    CAST(1 AS bit),
                    CAST(1 AS bit),
                    CAST(1 AS bit),
                    GETUTCDATE(),
                    NULL
                FROM [TenantUsers] tu
                LEFT JOIN [NotificationSettings] ns
                    ON ns.[TenantId] = tu.[TenantId]
                   AND ns.[UserId] = tu.[UserId]
                WHERE tu.[IsActive] = CAST(1 AS bit)
                  AND ns.[NotificationSettingId] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationSettings");
        }
    }
}

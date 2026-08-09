using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Platform.Migrations;

/// <inheritdoc />
public partial class InitialPlatformSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "platform");

        migrationBuilder.CreateTable(
            name: "Plans",
            schema: "platform",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                MonthlyPrice = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                AnnualPrice = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                FeaturesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Plans", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PlatformAdmins",
            schema: "platform",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlatformAdmins", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Tenants",
            schema: "platform",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Subdomain = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: false),
                SchemaName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                TrialEndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                SubscriptionExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ConnectionString = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tenants_Plans_PlanId",
                    column: x => x.PlanId,
                    principalSchema: "platform",
                    principalTable: "Plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Subscriptions",
            schema: "platform",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Interval = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Subscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Subscriptions_Plans_PlanId",
                    column: x => x.PlanId,
                    principalSchema: "platform",
                    principalTable: "Plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Subscriptions_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalSchema: "platform",
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Plans_Name",
            schema: "platform",
            table: "Plans",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PlatformAdmins_Email",
            schema: "platform",
            table: "PlatformAdmins",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_PlanId",
            schema: "platform",
            table: "Subscriptions",
            column: "PlanId");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_TenantId",
            schema: "platform",
            table: "Subscriptions",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_PlanId",
            schema: "platform",
            table: "Tenants",
            column: "PlanId");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_SchemaName",
            schema: "platform",
            table: "Tenants",
            column: "SchemaName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Subdomain",
            schema: "platform",
            table: "Tenants",
            column: "Subdomain",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PlatformAdmins",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "Subscriptions",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "Tenants",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "Plans",
            schema: "platform");
    }
}

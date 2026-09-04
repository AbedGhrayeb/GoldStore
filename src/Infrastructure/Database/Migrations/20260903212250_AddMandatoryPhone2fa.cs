using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddMandatoryPhone2fa : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "PhoneNumberVerified",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "TwoFactorEnabled",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "TwoFactorEnabledAtUtc",
            table: "Users",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PhoneNumber",
            table: "PlatformUsers",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "PhoneNumberVerified",
            table: "PlatformUsers",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "TwoFactorEnabled",
            table: "PlatformUsers",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "TwoFactorEnabledAtUtc",
            table: "PlatformUsers",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "PlatformRecoveryCodes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlatformUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                IsUsed = table.Column<bool>(type: "bit", nullable: false),
                UsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlatformRecoveryCodes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserRecoveryCodes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                IsUsed = table.Column<bool>(type: "bit", nullable: false),
                UsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRecoveryCodes", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserRecoveryCodes_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PlatformUsers_PhoneNumber",
            table: "PlatformUsers",
            column: "PhoneNumber",
            unique: true,
            filter: "[PhoneNumber] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_PlatformRecoveryCodes_PlatformUserId_CodeHash",
            table: "PlatformRecoveryCodes",
            columns: new[] { "PlatformUserId", "CodeHash" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserRecoveryCodes_TenantId",
            table: "UserRecoveryCodes",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_UserRecoveryCodes_UserId_CodeHash",
            table: "UserRecoveryCodes",
            columns: new[] { "UserId", "CodeHash" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PlatformRecoveryCodes");

        migrationBuilder.DropTable(
            name: "UserRecoveryCodes");

        migrationBuilder.DropIndex(
            name: "IX_PlatformUsers_PhoneNumber",
            table: "PlatformUsers");

        migrationBuilder.DropColumn(
            name: "PhoneNumberVerified",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "TwoFactorEnabled",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "TwoFactorEnabledAtUtc",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "PhoneNumber",
            table: "PlatformUsers");

        migrationBuilder.DropColumn(
            name: "PhoneNumberVerified",
            table: "PlatformUsers");

        migrationBuilder.DropColumn(
            name: "TwoFactorEnabled",
            table: "PlatformUsers");

        migrationBuilder.DropColumn(
            name: "TwoFactorEnabledAtUtc",
            table: "PlatformUsers");
    }
}

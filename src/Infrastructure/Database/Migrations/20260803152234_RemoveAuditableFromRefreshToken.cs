using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class RemoveAuditableFromRefreshToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreatedAtUtc",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "CreatedBy",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "LastModifiedBy",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "LastModifiedUtc",
            table: "RefreshTokens");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAtUtc",
            table: "RefreshTokens",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CreatedBy",
            table: "RefreshTokens",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "LastModifiedBy",
            table: "RefreshTokens",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastModifiedUtc",
            table: "RefreshTokens",
            type: "datetimeoffset",
            nullable: true);
    }
}

// <copyright file="20260906194340_CategoryWeightKaratRequired.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CategoryWeightKaratRequired : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Backfill rows created while weight/karat were optional. Weight 0 forces a
        // correction on the next edit (domain requires > 0); karat defaults to K21
        // (empty string would fail enum materialization on read).
        migrationBuilder.Sql("UPDATE categories SET weight_in_grams = 0 WHERE weight_in_grams IS NULL;");
        migrationBuilder.Sql("UPDATE categories SET karat = 'K21' WHERE karat IS NULL;");

        migrationBuilder.AlterColumn<decimal>(
            name: "weight_in_grams",
            table: "categories",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,3)",
            oldPrecision: 18,
            oldScale: 3,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "karat",
            table: "categories",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "K21",
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3,
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "weight_in_grams",
            table: "categories",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,3)",
            oldPrecision: 18,
            oldScale: 3);

        migrationBuilder.AlterColumn<string>(
            name: "karat",
            table: "categories",
            type: "character varying(3)",
            maxLength: 3,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3);
    }
}

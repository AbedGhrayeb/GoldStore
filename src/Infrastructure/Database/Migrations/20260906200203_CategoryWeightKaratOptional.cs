// <copyright file="20260906200203_CategoryWeightKaratOptional.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CategoryWeightKaratOptional : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
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
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3,
            oldNullable: true);
    }
}

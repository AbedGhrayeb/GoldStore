// <copyright file="20260907071913_DeliveryDueHeaderLevel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class DeliveryDueHeaderLevel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "amount_due",
            table: "supplier_deliveries");

        migrationBuilder.DropColumn(
            name: "amount_due_currency",
            table: "supplier_deliveries");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "amount_due",
            table: "supplier_deliveries",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "amount_due_currency",
            table: "supplier_deliveries",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: string.Empty);
    }
}

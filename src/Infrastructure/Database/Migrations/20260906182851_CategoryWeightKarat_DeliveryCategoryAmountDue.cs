// <copyright file="20260906182851_CategoryWeightKarat_DeliveryCategoryAmountDue.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CategoryWeightKarat_DeliveryCategoryAmountDue : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
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

        migrationBuilder.AddColumn<Guid>(
            name: "category_id",
            table: "supplier_deliveries",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "karat",
            table: "categories",
            type: "character varying(3)",
            maxLength: 3,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "weight_in_grams",
            table: "categories",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_supplier_deliveries_category_id",
            table: "supplier_deliveries",
            column: "category_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_deliveries_tenant_id_category_id",
            table: "supplier_deliveries",
            columns: new[] { "tenant_id", "category_id" });

        migrationBuilder.AddForeignKey(
            name: "fk_supplier_deliveries_categories_category_id",
            table: "supplier_deliveries",
            column: "category_id",
            principalTable: "categories",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_supplier_deliveries_categories_category_id",
            table: "supplier_deliveries");

        migrationBuilder.DropIndex(
            name: "ix_supplier_deliveries_category_id",
            table: "supplier_deliveries");

        migrationBuilder.DropIndex(
            name: "ix_supplier_deliveries_tenant_id_category_id",
            table: "supplier_deliveries");

        migrationBuilder.DropColumn(
            name: "amount_due",
            table: "supplier_deliveries");

        migrationBuilder.DropColumn(
            name: "amount_due_currency",
            table: "supplier_deliveries");

        migrationBuilder.DropColumn(
            name: "category_id",
            table: "supplier_deliveries");

        migrationBuilder.DropColumn(
            name: "karat",
            table: "categories");

        migrationBuilder.DropColumn(
            name: "weight_in_grams",
            table: "categories");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddSupplierEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "suppliers",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                primary_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                secondary_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                bank_account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_suppliers", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "supplier_gold_ledger_entries",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_gold_ledger_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_gold_ledger_entries_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalSchema: "public",
                    principalTable: "suppliers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_gold_ledger_entries_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "public",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_manufacturing_ledger_entries",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_manufacturing_ledger_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_manufacturing_ledger_entries_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalSchema: "public",
                    principalTable: "suppliers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_manufacturing_ledger_entries_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "public",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_supplier_gold_ledger_entries_reference_type_reference_id",
            schema: "public",
            table: "supplier_gold_ledger_entries",
            columns: ["reference_type", "reference_id"]);

        migrationBuilder.CreateIndex(
            name: "ix_supplier_gold_ledger_entries_supplier_id_date",
            schema: "public",
            table: "supplier_gold_ledger_entries",
            columns: ["supplier_id", "date"]);

        migrationBuilder.CreateIndex(
            name: "ix_supplier_gold_ledger_entries_user_id",
            schema: "public",
            table: "supplier_gold_ledger_entries",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_manufacturing_ledger_entries_reference_type_refere",
            schema: "public",
            table: "supplier_manufacturing_ledger_entries",
            columns: ["reference_type", "reference_id"]);

        migrationBuilder.CreateIndex(
            name: "ix_supplier_manufacturing_ledger_entries_supplier_id_date",
            schema: "public",
            table: "supplier_manufacturing_ledger_entries",
            columns: ["supplier_id", "date"]);

        migrationBuilder.CreateIndex(
            name: "ix_supplier_manufacturing_ledger_entries_user_id",
            schema: "public",
            table: "supplier_manufacturing_ledger_entries",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_suppliers_name",
            schema: "public",
            table: "suppliers",
            column: "name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "supplier_gold_ledger_entries",
            schema: "public");

        migrationBuilder.DropTable(
            name: "supplier_manufacturing_ledger_entries",
            schema: "public");

        migrationBuilder.DropTable(
            name: "suppliers",
            schema: "public");
    }
}

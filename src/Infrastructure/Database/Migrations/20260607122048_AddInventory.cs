using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;
    /// <inheritdoc />
    public partial class AddInventory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gold_ledger_entries",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
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
                table.PrimaryKey("pk_gold_ledger_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_gold_ledger_entries_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "public",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_gold_ledger_entries_karat_date",
            schema: "public",
            table: "gold_ledger_entries",
            columns: ["karat", "date"]);

        migrationBuilder.CreateIndex(
            name: "ix_gold_ledger_entries_reference_type_reference_id",
            schema: "public",
            table: "gold_ledger_entries",
            columns: ["reference_type", "reference_id"]);

        migrationBuilder.CreateIndex(
            name: "ix_gold_ledger_entries_user_id",
            schema: "public",
            table: "gold_ledger_entries",
            column: "user_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "gold_ledger_entries",
            schema: "public");
    }
}


using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddSupplierOperationEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
                name: "supplier_deliveries",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    manufacturing_fee_per_gram = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    total_manufacturing_fee = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    manufacturing_fee_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_deliveries_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "public",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_deliveries_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_manufacturing_payments",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_manufacturing_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_manufacturing_payments_financial_accounts_account_",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_manufacturing_payments_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "public",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_manufacturing_payments_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_scrap_gold_payments",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_scrap_gold_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_scrap_gold_payments_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "public",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_scrap_gold_payments_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_deliveries_supplier_id_date",
                schema: "public",
                table: "supplier_deliveries",
                columns: ["supplier_id", "date"]);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_deliveries_user_id",
                schema: "public",
                table: "supplier_deliveries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_manufacturing_payments_account_id",
                schema: "public",
                table: "supplier_manufacturing_payments",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_manufacturing_payments_supplier_id_date",
                schema: "public",
                table: "supplier_manufacturing_payments",
                columns: ["supplier_id", "date"]);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_manufacturing_payments_user_id",
                schema: "public",
                table: "supplier_manufacturing_payments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_scrap_gold_payments_supplier_id_date",
                schema: "public",
                table: "supplier_scrap_gold_payments",
                columns: ["supplier_id", "date"]);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_scrap_gold_payments_user_id",
                schema: "public",
                table: "supplier_scrap_gold_payments",
                column: "user_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "supplier_deliveries",
            schema: "public");

        migrationBuilder.DropTable(
            name: "supplier_manufacturing_payments",
            schema: "public");

        migrationBuilder.DropTable(
            name: "supplier_scrap_gold_payments",
            schema: "public");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddFinacial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "financial_accounts",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                account_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_financial_accounts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "financial_transactions",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_financial_transactions", x => x.id);
                table.ForeignKey(
                    name: "fk_financial_transactions_financial_accounts_account_id",
                    column: x => x.account_id,
                    principalSchema: "public",
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_financial_transactions_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "public",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_financial_accounts_name_currency",
            schema: "public",
            table: "financial_accounts",
            columns: ["name", "currency"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_financial_transactions_account_id_date",
            schema: "public",
            table: "financial_transactions",
            columns: [ "account_id", "date" ]);

        migrationBuilder.CreateIndex(
            name: "ix_financial_transactions_reference_type_reference_id",
            schema: "public",
            table: "financial_transactions",
            columns: ["reference_type", "reference_id" ]);

        migrationBuilder.CreateIndex(
            name: "ix_financial_transactions_user_id",
            schema: "public",
            table: "financial_transactions",
            column: "user_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "financial_transactions",
            schema: "public");

        migrationBuilder.DropTable(
            name: "financial_accounts",
            schema: "public");
    }
}

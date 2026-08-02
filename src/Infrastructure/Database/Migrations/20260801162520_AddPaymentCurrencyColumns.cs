using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddPaymentCurrencyColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "BaseAmount",
            table: "FinancialTransactions",
            type: "decimal(18,3)",
            precision: 18,
            scale: 3,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ExchangeRate",
            table: "FinancialTransactions",
            type: "decimal(18,6)",
            precision: 18,
            scale: 6,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BaseAmount",
            table: "FinancialTransactions");

        migrationBuilder.DropColumn(
            name: "ExchangeRate",
            table: "FinancialTransactions");
    }
}

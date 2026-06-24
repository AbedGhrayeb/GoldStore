using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.src.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerPurchaseInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CustomerPurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "CustomerPurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "RemainingBalance",
                table: "CustomerPurchaseInvoices");

            migrationBuilder.AlterColumn<string>(
                name: "SellerPhone",
                table: "CustomerPurchaseInvoices",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerName",
                table: "CustomerPurchaseInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "SeelerYearOfBirth",
                table: "CustomerPurchaseInvoices",
                type: "int",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAccountNumber",
                table: "CustomerPurchaseInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddress",
                table: "CustomerPurchaseInvoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerIdNumber",
                table: "CustomerPurchaseInvoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeelerYearOfBirth",
                table: "CustomerPurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "SellerAccountNumber",
                table: "CustomerPurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "SellerAddress",
                table: "CustomerPurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "SellerIdNumber",
                table: "CustomerPurchaseInvoices");

            migrationBuilder.AlterColumn<string>(
                name: "SellerPhone",
                table: "CustomerPurchaseInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(13)",
                oldMaxLength: 13,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerName",
                table: "CustomerPurchaseInvoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CustomerPurchaseInvoices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "CustomerPurchaseInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingBalance",
                table: "CustomerPurchaseInvoices",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }
    }
}

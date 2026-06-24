using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromSupplierLedgerEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierDeliveries_Users_UserId",
                table: "SupplierDeliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierGoldLedgerEntries_Users_UserId",
                table: "SupplierGoldLedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierManufacturingLedgerEntries_Users_UserId",
                table: "SupplierManufacturingLedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierManufacturingPayments_Users_UserId",
                table: "SupplierManufacturingPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierScrapGoldPayments_Users_UserId",
                table: "SupplierScrapGoldPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierScrapGoldPayments_UserId",
                table: "SupplierScrapGoldPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierManufacturingPayments_UserId",
                table: "SupplierManufacturingPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierManufacturingLedgerEntries_UserId",
                table: "SupplierManufacturingLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_SupplierGoldLedgerEntries_UserId",
                table: "SupplierGoldLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_SupplierDeliveries_UserId",
                table: "SupplierDeliveries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SupplierScrapGoldPayments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SupplierManufacturingPayments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SupplierManufacturingLedgerEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SupplierGoldLedgerEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SupplierDeliveries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SupplierScrapGoldPayments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SupplierManufacturingPayments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SupplierManufacturingLedgerEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SupplierGoldLedgerEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SupplierDeliveries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SupplierScrapGoldPayments_UserId",
                table: "SupplierScrapGoldPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturingPayments_UserId",
                table: "SupplierManufacturingPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturingLedgerEntries_UserId",
                table: "SupplierManufacturingLedgerEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierGoldLedgerEntries_UserId",
                table: "SupplierGoldLedgerEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDeliveries_UserId",
                table: "SupplierDeliveries",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierDeliveries_Users_UserId",
                table: "SupplierDeliveries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierGoldLedgerEntries_Users_UserId",
                table: "SupplierGoldLedgerEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierManufacturingLedgerEntries_Users_UserId",
                table: "SupplierManufacturingLedgerEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierManufacturingPayments_Users_UserId",
                table: "SupplierManufacturingPayments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierScrapGoldPayments_Users_UserId",
                table: "SupplierScrapGoldPayments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

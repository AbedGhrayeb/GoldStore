using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.src.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SupplierFinanialTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierFinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierFinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialTransactions_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialTransactions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierFinancialLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierFinancialTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierFinancialLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialLedgerEntries_SupplierFinancialTransactions_SupplierFinancialTransactionId",
                        column: x => x.SupplierFinancialTransactionId,
                        principalTable: "SupplierFinancialTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierFinancialPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierFinancialTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierFinancialPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialPayments_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialPayments_SupplierFinancialTransactions_SupplierFinancialTransactionId",
                        column: x => x.SupplierFinancialTransactionId,
                        principalTable: "SupplierFinancialTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialLedgerEntries_MovementType",
                table: "SupplierFinancialLedgerEntries",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialLedgerEntries_SupplierFinancialTransactionId_Date",
                table: "SupplierFinancialLedgerEntries",
                columns: new[] { "SupplierFinancialTransactionId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialPayments_AccountId",
                table: "SupplierFinancialPayments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialPayments_SupplierFinancialTransactionId_Date",
                table: "SupplierFinancialPayments",
                columns: new[] { "SupplierFinancialTransactionId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialTransactions_AccountId",
                table: "SupplierFinancialTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialTransactions_SupplierId_CreatedAt",
                table: "SupplierFinancialTransactions",
                columns: new[] { "SupplierId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierFinancialLedgerEntries");

            migrationBuilder.DropTable(
                name: "SupplierFinancialPayments");

            migrationBuilder.DropTable(
                name: "SupplierFinancialTransactions");
        }
    }
}

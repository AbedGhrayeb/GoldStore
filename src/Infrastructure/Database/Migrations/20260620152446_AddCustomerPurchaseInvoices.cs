using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPurchaseInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerPurchaseInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SellerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SellerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BuyerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPurchaseInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPurchaseInvoices_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPurchaseInvoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPurchaseInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerPurchaseInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PricePerGram = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    GoldAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPurchaseInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPurchaseInvoiceItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPurchaseInvoiceItems_CustomerPurchaseInvoices_CustomerPurchaseInvoiceId",
                        column: x => x.CustomerPurchaseInvoiceId,
                        principalTable: "CustomerPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoiceItems_CategoryId",
                table: "CustomerPurchaseInvoiceItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoiceItems_CustomerPurchaseInvoiceId",
                table: "CustomerPurchaseInvoiceItems",
                column: "CustomerPurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoices_AccountId",
                table: "CustomerPurchaseInvoices",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoices_Date",
                table: "CustomerPurchaseInvoices",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoices_InvoiceNumber",
                table: "CustomerPurchaseInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoices_UserId",
                table: "CustomerPurchaseInvoices",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPurchaseInvoiceItems");

            migrationBuilder.DropTable(
                name: "CustomerPurchaseInvoices");
        }
    }
}

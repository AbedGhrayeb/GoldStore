using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class UseSqlServer : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
                table.ForeignKey(
                    name: "FK_Categories_Categories_ParentCategoryId",
                    column: x => x.ParentCategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "FinancialAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                AccountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PrimaryPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                SecondaryPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                BankAccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suppliers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FinancialTransactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_FinancialTransactions_FinancialAccounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "FinancialAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_FinancialTransactions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "GoldLedgerEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GoldLedgerEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_GoldLedgerEntries_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SupplierDeliveries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                ManufacturingFeePerGram = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                TotalManufacturingFee = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                ManufacturingFeeCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierDeliveries", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierDeliveries_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierDeliveries_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SupplierGoldLedgerEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierGoldLedgerEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierGoldLedgerEntries_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierGoldLedgerEntries_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SupplierManufacturingLedgerEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierManufacturingLedgerEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierManufacturingLedgerEntries_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierManufacturingLedgerEntries_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SupplierManufacturingPayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierManufacturingPayments", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierManufacturingPayments_FinancialAccounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "FinancialAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierManufacturingPayments_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierManufacturingPayments_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SupplierScrapGoldPayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierScrapGoldPayments", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierScrapGoldPayments_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierScrapGoldPayments_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TodoItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                Labels = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Priority = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_TodoItems_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            table: "Categories",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Categories_ParentCategoryId_Name",
            table: "Categories",
            columns: ["ParentCategoryId", "Name"],
            unique: true,
            filter: "[ParentCategoryId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_FinancialAccounts_Name_Currency",
            table: "FinancialAccounts",
            columns: ["Name", "Currency"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_AccountId_Date",
            table: "FinancialTransactions",
            columns: ["AccountId", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_ReferenceType_ReferenceId",
            table: "FinancialTransactions",
            columns: ["ReferenceType", "ReferenceId"]);

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_UserId",
            table: "FinancialTransactions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_GoldLedgerEntries_Karat_Date",
            table: "GoldLedgerEntries",
            columns: ["Karat", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_GoldLedgerEntries_ReferenceType_ReferenceId",
            table: "GoldLedgerEntries",
            columns: ["ReferenceType", "ReferenceId"]);

        migrationBuilder.CreateIndex(
            name: "IX_GoldLedgerEntries_UserId",
            table: "GoldLedgerEntries",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierDeliveries_SupplierId_Date",
            table: "SupplierDeliveries",
            columns: ["SupplierId", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierDeliveries_UserId",
            table: "SupplierDeliveries",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierGoldLedgerEntries_ReferenceType_ReferenceId",
            table: "SupplierGoldLedgerEntries",
            columns: ["ReferenceType", "ReferenceId"]);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierGoldLedgerEntries_SupplierId_Date",
            table: "SupplierGoldLedgerEntries",
            columns: ["SupplierId", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierGoldLedgerEntries_UserId",
            table: "SupplierGoldLedgerEntries",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingLedgerEntries_ReferenceType_ReferenceId",
            table: "SupplierManufacturingLedgerEntries",
            columns: ["ReferenceType", "ReferenceId"]);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingLedgerEntries_SupplierId_Date",
            table: "SupplierManufacturingLedgerEntries",
            columns: ["SupplierId", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingLedgerEntries_UserId",
            table: "SupplierManufacturingLedgerEntries",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingPayments_AccountId",
            table: "SupplierManufacturingPayments",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingPayments_SupplierId_Date",
            table: "SupplierManufacturingPayments",
            columns: ["SupplierId", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingPayments_UserId",
            table: "SupplierManufacturingPayments",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Suppliers_Name",
            table: "Suppliers",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierScrapGoldPayments_SupplierId_Date",
            table: "SupplierScrapGoldPayments",
            columns: ["SupplierId", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierScrapGoldPayments_UserId",
            table: "SupplierScrapGoldPayments",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_TodoItems_UserId",
            table: "TodoItems",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Categories");

        migrationBuilder.DropTable(
            name: "FinancialTransactions");

        migrationBuilder.DropTable(
            name: "GoldLedgerEntries");

        migrationBuilder.DropTable(
            name: "SupplierDeliveries");

        migrationBuilder.DropTable(
            name: "SupplierGoldLedgerEntries");

        migrationBuilder.DropTable(
            name: "SupplierManufacturingLedgerEntries");

        migrationBuilder.DropTable(
            name: "SupplierManufacturingPayments");

        migrationBuilder.DropTable(
            name: "SupplierScrapGoldPayments");

        migrationBuilder.DropTable(
            name: "TodoItems");

        migrationBuilder.DropTable(
            name: "FinancialAccounts");

        migrationBuilder.DropTable(
            name: "Suppliers");

        migrationBuilder.DropTable(
            name: "Users");
    }
}


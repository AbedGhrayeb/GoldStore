using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialTenantSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "$tenant");

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "$tenant",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldLedgerEntries",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAdjustments",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrimaryPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SecondaryPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Debts",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Debts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Debts_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalSchema: "$tenant",
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransactions",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierDeliveries",
                schema: "$tenant",
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
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierDeliveries_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "$tenant",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierFinancialTransactions",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierFinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialTransactions_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialTransactions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "$tenant",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierGoldLedgerEntries",
                schema: "$tenant",
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
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierGoldLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierGoldLedgerEntries_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "$tenant",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierManufacturingLedgerEntries",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierManufacturingLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierManufacturingLedgerEntries_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "$tenant",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierManufacturingPayments",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierManufacturingPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierManufacturingPayments_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierManufacturingPayments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "$tenant",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierScrapGoldPayments",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierScrapGoldPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierScrapGoldPayments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "$tenant",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SalaryCycle = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "$tenant",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DebtLedgerEntries",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebtLedgerEntries_Debts_DebtId",
                        column: x => x.DebtId,
                        principalSchema: "$tenant",
                        principalTable: "Debts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierFinancialLedgerEntries",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierFinancialTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierFinancialLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialLedgerEntries_SupplierFinancialTransactions_SupplierFinancialTransactionId",
                        column: x => x.SupplierFinancialTransactionId,
                        principalSchema: "$tenant",
                        principalTable: "SupplierFinancialTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierFinancialPayments",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierFinancialTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierFinancialPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialPayments_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierFinancialPayments_SupplierFinancialTransactions_SupplierFinancialTransactionId",
                        column: x => x.SupplierFinancialTransactionId,
                        principalSchema: "$tenant",
                        principalTable: "SupplierFinancialTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPurchaseInvoices",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SellerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SellerIdNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SellerPhone = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    SellerYearOfBirth = table.Column<int>(type: "int", maxLength: 13, nullable: true),
                    SellerAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerAccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPurchaseInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPurchaseInvoices_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "$tenant",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPurchaseInvoices_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryPayments",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SalaryAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryPayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "$tenant",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPayments_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CustomerAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "$tenant",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "$tenant",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPurchaseInvoiceItems",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerPurchaseInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PricePerGram = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPurchaseInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPurchaseInvoiceItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "$tenant",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPurchaseInvoiceItems_CustomerPurchaseInvoices_CustomerPurchaseInvoiceId",
                        column: x => x.CustomerPurchaseInvoiceId,
                        principalSchema: "$tenant",
                        principalTable: "CustomerPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceItems",
                schema: "$tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Karat = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    WeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Equivalent21KWeightInGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PricePerGram = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    GoldAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "$tenant",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceItems_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalSchema: "$tenant",
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                schema: "$tenant",
                table: "Categories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId_Name",
                schema: "$tenant",
                table: "Categories",
                columns: new[] { "ParentCategoryId", "Name" },
                unique: true,
                filter: "[ParentCategoryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoiceItems_CategoryId",
                schema: "$tenant",
                table: "CustomerPurchaseInvoiceItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoiceItems_CustomerPurchaseInvoiceId",
                schema: "$tenant",
                table: "CustomerPurchaseInvoiceItems",
                column: "CustomerPurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoices_AccountId",
                schema: "$tenant",
                table: "CustomerPurchaseInvoices",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoices_Date",
                schema: "$tenant",
                table: "CustomerPurchaseInvoices",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoices_EmployeeId",
                schema: "$tenant",
                table: "CustomerPurchaseInvoices",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPurchaseInvoices_InvoiceNumber",
                schema: "$tenant",
                table: "CustomerPurchaseInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebtLedgerEntries_DebtId_CreatedAtUtc",
                schema: "$tenant",
                table: "DebtLedgerEntries",
                columns: new[] { "DebtId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Debts_AccountId",
                schema: "$tenant",
                table: "Debts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Debts_CreatedAtUtc",
                schema: "$tenant",
                table: "Debts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Debts_Name",
                schema: "$tenant",
                table: "Debts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                schema: "$tenant",
                table: "Employees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Name",
                schema: "$tenant",
                table: "ExpenseCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_AccountId_ExpenseDate",
                schema: "$tenant",
                table: "Expenses",
                columns: new[] { "AccountId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseCategoryId_ExpenseDate",
                schema: "$tenant",
                table: "Expenses",
                columns: new[] { "ExpenseCategoryId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                schema: "$tenant",
                table: "Expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_Name_Currency",
                schema: "$tenant",
                table: "FinancialAccounts",
                columns: new[] { "Name", "Currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_AccountId_CreatedAtUtc",
                schema: "$tenant",
                table: "FinancialTransactions",
                columns: new[] { "AccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_ReferenceType_ReferenceId",
                schema: "$tenant",
                table: "FinancialTransactions",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldLedgerEntries_Karat_CreatedAtUtc",
                schema: "$tenant",
                table: "GoldLedgerEntries",
                columns: new[] { "Karat", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldLedgerEntries_ReferenceType_ReferenceId",
                schema: "$tenant",
                table: "GoldLedgerEntries",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_CreatedAtUtc",
                schema: "$tenant",
                table: "InventoryAdjustments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_Type_CreatedAtUtc",
                schema: "$tenant",
                table: "InventoryAdjustments",
                columns: new[] { "Type", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                schema: "$tenant",
                table: "RefreshTokens",
                column: "Token",
                unique: true,
                filter: "[Token] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_AccountId",
                schema: "$tenant",
                table: "SalaryPayments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_EmployeeId_PaymentDate",
                schema: "$tenant",
                table: "SalaryPayments",
                columns: new[] { "EmployeeId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_PaymentDate",
                schema: "$tenant",
                table: "SalaryPayments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_CategoryId",
                schema: "$tenant",
                table: "SalesInvoiceItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_SalesInvoiceId",
                schema: "$tenant",
                table: "SalesInvoiceItems",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_AccountId",
                schema: "$tenant",
                table: "SalesInvoices",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_Date",
                schema: "$tenant",
                table: "SalesInvoices",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_EmployeeId",
                schema: "$tenant",
                table: "SalesInvoices",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_InvoiceNumber",
                schema: "$tenant",
                table: "SalesInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDeliveries_SupplierId_CreatedAtUtc",
                schema: "$tenant",
                table: "SupplierDeliveries",
                columns: new[] { "SupplierId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialLedgerEntries_SupplierFinancialTransactionId_CreatedAtUtc",
                schema: "$tenant",
                table: "SupplierFinancialLedgerEntries",
                columns: new[] { "SupplierFinancialTransactionId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialPayments_AccountId",
                schema: "$tenant",
                table: "SupplierFinancialPayments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialPayments_SupplierFinancialTransactionId_CreatedAtUtc",
                schema: "$tenant",
                table: "SupplierFinancialPayments",
                columns: new[] { "SupplierFinancialTransactionId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialTransactions_AccountId",
                schema: "$tenant",
                table: "SupplierFinancialTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFinancialTransactions_SupplierId_CreatedAtUtc",
                schema: "$tenant",
                table: "SupplierFinancialTransactions",
                columns: new[] { "SupplierId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierGoldLedgerEntries_SupplierId_CreatedAtUtc",
                schema: "$tenant",
                table: "SupplierGoldLedgerEntries",
                columns: new[] { "SupplierId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturingLedgerEntries_SupplierId_CreatedAtUtc",
                schema: "$tenant",
                table: "SupplierManufacturingLedgerEntries",
                columns: new[] { "SupplierId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturingPayments_AccountId",
                schema: "$tenant",
                table: "SupplierManufacturingPayments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturingPayments_SupplierId_CreatedAtUtc",
                schema: "$tenant",
                table: "SupplierManufacturingPayments",
                columns: new[] { "SupplierId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                schema: "$tenant",
                table: "Suppliers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierScrapGoldPayments_SupplierId_CreatedAtUtc",
                schema: "$tenant",
                table: "SupplierScrapGoldPayments",
                columns: new[] { "SupplierId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "$tenant",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPurchaseInvoiceItems",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "DebtLedgerEntries",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "Expenses",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "FinancialTransactions",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "GoldLedgerEntries",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "InventoryAdjustments",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SalaryPayments",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SalesInvoiceItems",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SupplierDeliveries",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SupplierFinancialLedgerEntries",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SupplierFinancialPayments",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SupplierGoldLedgerEntries",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SupplierManufacturingLedgerEntries",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SupplierManufacturingPayments",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SupplierScrapGoldPayments",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "CustomerPurchaseInvoices",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "Debts",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "ExpenseCategories",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SalesInvoices",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "SupplierFinancialTransactions",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "FinancialAccounts",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "$tenant");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "$tenant");
        }
    }
}

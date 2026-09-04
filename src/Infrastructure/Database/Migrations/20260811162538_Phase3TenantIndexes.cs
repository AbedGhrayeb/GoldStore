using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Phase3TenantIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SupplierScrapGoldPayments_SupplierId_CreatedAtUtc",
            table: "SupplierScrapGoldPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierScrapGoldPayments_TenantId",
            table: "SupplierScrapGoldPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierManufacturingPayments_SupplierId_CreatedAtUtc",
            table: "SupplierManufacturingPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierManufacturingPayments_TenantId",
            table: "SupplierManufacturingPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierManufacturingLedgerEntries_SupplierId_CreatedAtUtc",
            table: "SupplierManufacturingLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierManufacturingLedgerEntries_TenantId",
            table: "SupplierManufacturingLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierGoldLedgerEntries_SupplierId_CreatedAtUtc",
            table: "SupplierGoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierGoldLedgerEntries_TenantId",
            table: "SupplierGoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialTransactions_SupplierId_CreatedAtUtc",
            table: "SupplierFinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialTransactions_TenantId",
            table: "SupplierFinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialPayments_SupplierFinancialTransactionId_CreatedAtUtc",
            table: "SupplierFinancialPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialPayments_TenantId",
            table: "SupplierFinancialPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialLedgerEntries_SupplierFinancialTransactionId_CreatedAtUtc",
            table: "SupplierFinancialLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialLedgerEntries_TenantId",
            table: "SupplierFinancialLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierDeliveries_SupplierId_CreatedAtUtc",
            table: "SupplierDeliveries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierDeliveries_TenantId",
            table: "SupplierDeliveries");

        migrationBuilder.DropIndex(
            name: "IX_SalesInvoiceItems_TenantId",
            table: "SalesInvoiceItems");

        migrationBuilder.DropIndex(
            name: "IX_SalaryPayments_EmployeeId_PaymentDate",
            table: "SalaryPayments");

        migrationBuilder.DropIndex(
            name: "IX_SalaryPayments_PaymentDate",
            table: "SalaryPayments");

        migrationBuilder.DropIndex(
            name: "IX_SalaryPayments_TenantId",
            table: "SalaryPayments");

        migrationBuilder.DropIndex(
            name: "IX_InventoryAdjustments_CreatedAtUtc",
            table: "InventoryAdjustments");

        migrationBuilder.DropIndex(
            name: "IX_InventoryAdjustments_TenantId",
            table: "InventoryAdjustments");

        migrationBuilder.DropIndex(
            name: "IX_InventoryAdjustments_Type_CreatedAtUtc",
            table: "InventoryAdjustments");

        migrationBuilder.DropIndex(
            name: "IX_GoldLedgerEntries_Karat_CreatedAtUtc",
            table: "GoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_GoldLedgerEntries_ReferenceType_ReferenceId",
            table: "GoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_GoldLedgerEntries_TenantId",
            table: "GoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_FinancialTransactions_AccountId_CreatedAtUtc",
            table: "FinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_FinancialTransactions_ReferenceType_ReferenceId",
            table: "FinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_FinancialTransactions_TenantId",
            table: "FinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_AccountId_ExpenseDate",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_ExpenseCategoryId_ExpenseDate",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_ExpenseDate",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_TenantId",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Debts_CreatedAtUtc",
            table: "Debts");

        migrationBuilder.DropIndex(
            name: "IX_Debts_Name",
            table: "Debts");

        migrationBuilder.DropIndex(
            name: "IX_Debts_TenantId",
            table: "Debts");

        migrationBuilder.DropIndex(
            name: "IX_DebtLedgerEntries_DebtId_CreatedAtUtc",
            table: "DebtLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_DebtLedgerEntries_TenantId",
            table: "DebtLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_CustomerPurchaseInvoiceItems_TenantId",
            table: "CustomerPurchaseInvoiceItems");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierScrapGoldPayments_SupplierId",
            table: "SupplierScrapGoldPayments",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierScrapGoldPayments_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierScrapGoldPayments",
            columns: new[] { "TenantId", "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingPayments_SupplierId",
            table: "SupplierManufacturingPayments",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingPayments_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierManufacturingPayments",
            columns: new[] { "TenantId", "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingLedgerEntries_SupplierId",
            table: "SupplierManufacturingLedgerEntries",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingLedgerEntries_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierManufacturingLedgerEntries",
            columns: new[] { "TenantId", "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierGoldLedgerEntries_SupplierId",
            table: "SupplierGoldLedgerEntries",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierGoldLedgerEntries_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierGoldLedgerEntries",
            columns: new[] { "TenantId", "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialTransactions_SupplierId",
            table: "SupplierFinancialTransactions",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialTransactions_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierFinancialTransactions",
            columns: new[] { "TenantId", "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialPayments_SupplierFinancialTransactionId",
            table: "SupplierFinancialPayments",
            column: "SupplierFinancialTransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialPayments_TenantId_SupplierFinancialTransactionId_CreatedAtUtc",
            table: "SupplierFinancialPayments",
            columns: new[] { "TenantId", "SupplierFinancialTransactionId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialLedgerEntries_SupplierFinancialTransactionId",
            table: "SupplierFinancialLedgerEntries",
            column: "SupplierFinancialTransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialLedgerEntries_TenantId_SupplierFinancialTransactionId_CreatedAtUtc",
            table: "SupplierFinancialLedgerEntries",
            columns: new[] { "TenantId", "SupplierFinancialTransactionId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierDeliveries_SupplierId",
            table: "SupplierDeliveries",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierDeliveries_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierDeliveries",
            columns: new[] { "TenantId", "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SalesInvoiceItems_TenantId_SalesInvoiceId",
            table: "SalesInvoiceItems",
            columns: new[] { "TenantId", "SalesInvoiceId" });

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_EmployeeId",
            table: "SalaryPayments",
            column: "EmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_TenantId_EmployeeId_PaymentDate",
            table: "SalaryPayments",
            columns: new[] { "TenantId", "EmployeeId", "PaymentDate" });

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_TenantId_PaymentDate",
            table: "SalaryPayments",
            columns: new[] { "TenantId", "PaymentDate" });

        migrationBuilder.CreateIndex(
            name: "IX_InventoryAdjustments_TenantId_CreatedAtUtc",
            table: "InventoryAdjustments",
            columns: new[] { "TenantId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_InventoryAdjustments_TenantId_Type_CreatedAtUtc",
            table: "InventoryAdjustments",
            columns: new[] { "TenantId", "Type", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_GoldLedgerEntries_TenantId_Karat_CreatedAtUtc",
            table: "GoldLedgerEntries",
            columns: new[] { "TenantId", "Karat", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_GoldLedgerEntries_TenantId_ReferenceType_ReferenceId",
            table: "GoldLedgerEntries",
            columns: new[] { "TenantId", "ReferenceType", "ReferenceId" });

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_AccountId",
            table: "FinancialTransactions",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_TenantId_AccountId_CreatedAtUtc",
            table: "FinancialTransactions",
            columns: new[] { "TenantId", "AccountId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_TenantId_ReferenceType_ReferenceId",
            table: "FinancialTransactions",
            columns: new[] { "TenantId", "ReferenceType", "ReferenceId" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_AccountId",
            table: "Expenses",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_ExpenseCategoryId",
            table: "Expenses",
            column: "ExpenseCategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_TenantId_AccountId_ExpenseDate",
            table: "Expenses",
            columns: new[] { "TenantId", "AccountId", "ExpenseDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_TenantId_ExpenseCategoryId_ExpenseDate",
            table: "Expenses",
            columns: new[] { "TenantId", "ExpenseCategoryId", "ExpenseDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_TenantId_ExpenseDate",
            table: "Expenses",
            columns: new[] { "TenantId", "ExpenseDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Debts_TenantId_CreatedAtUtc",
            table: "Debts",
            columns: new[] { "TenantId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Debts_TenantId_Name",
            table: "Debts",
            columns: new[] { "TenantId", "Name" });

        migrationBuilder.CreateIndex(
            name: "IX_DebtLedgerEntries_DebtId",
            table: "DebtLedgerEntries",
            column: "DebtId");

        migrationBuilder.CreateIndex(
            name: "IX_DebtLedgerEntries_TenantId_DebtId_CreatedAtUtc",
            table: "DebtLedgerEntries",
            columns: new[] { "TenantId", "DebtId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerPurchaseInvoiceItems_TenantId_CustomerPurchaseInvoiceId",
            table: "CustomerPurchaseInvoiceItems",
            columns: new[] { "TenantId", "CustomerPurchaseInvoiceId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SupplierScrapGoldPayments_SupplierId",
            table: "SupplierScrapGoldPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierScrapGoldPayments_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierScrapGoldPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierManufacturingPayments_SupplierId",
            table: "SupplierManufacturingPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierManufacturingPayments_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierManufacturingPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierManufacturingLedgerEntries_SupplierId",
            table: "SupplierManufacturingLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierManufacturingLedgerEntries_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierManufacturingLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierGoldLedgerEntries_SupplierId",
            table: "SupplierGoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierGoldLedgerEntries_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierGoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialTransactions_SupplierId",
            table: "SupplierFinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialTransactions_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierFinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialPayments_SupplierFinancialTransactionId",
            table: "SupplierFinancialPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialPayments_TenantId_SupplierFinancialTransactionId_CreatedAtUtc",
            table: "SupplierFinancialPayments");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialLedgerEntries_SupplierFinancialTransactionId",
            table: "SupplierFinancialLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierFinancialLedgerEntries_TenantId_SupplierFinancialTransactionId_CreatedAtUtc",
            table: "SupplierFinancialLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierDeliveries_SupplierId",
            table: "SupplierDeliveries");

        migrationBuilder.DropIndex(
            name: "IX_SupplierDeliveries_TenantId_SupplierId_CreatedAtUtc",
            table: "SupplierDeliveries");

        migrationBuilder.DropIndex(
            name: "IX_SalesInvoiceItems_TenantId_SalesInvoiceId",
            table: "SalesInvoiceItems");

        migrationBuilder.DropIndex(
            name: "IX_SalaryPayments_EmployeeId",
            table: "SalaryPayments");

        migrationBuilder.DropIndex(
            name: "IX_SalaryPayments_TenantId_EmployeeId_PaymentDate",
            table: "SalaryPayments");

        migrationBuilder.DropIndex(
            name: "IX_SalaryPayments_TenantId_PaymentDate",
            table: "SalaryPayments");

        migrationBuilder.DropIndex(
            name: "IX_InventoryAdjustments_TenantId_CreatedAtUtc",
            table: "InventoryAdjustments");

        migrationBuilder.DropIndex(
            name: "IX_InventoryAdjustments_TenantId_Type_CreatedAtUtc",
            table: "InventoryAdjustments");

        migrationBuilder.DropIndex(
            name: "IX_GoldLedgerEntries_TenantId_Karat_CreatedAtUtc",
            table: "GoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_GoldLedgerEntries_TenantId_ReferenceType_ReferenceId",
            table: "GoldLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_FinancialTransactions_AccountId",
            table: "FinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_FinancialTransactions_TenantId_AccountId_CreatedAtUtc",
            table: "FinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_FinancialTransactions_TenantId_ReferenceType_ReferenceId",
            table: "FinancialTransactions");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_AccountId",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_ExpenseCategoryId",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_TenantId_AccountId_ExpenseDate",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_TenantId_ExpenseCategoryId_ExpenseDate",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_TenantId_ExpenseDate",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Debts_TenantId_CreatedAtUtc",
            table: "Debts");

        migrationBuilder.DropIndex(
            name: "IX_Debts_TenantId_Name",
            table: "Debts");

        migrationBuilder.DropIndex(
            name: "IX_DebtLedgerEntries_DebtId",
            table: "DebtLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_DebtLedgerEntries_TenantId_DebtId_CreatedAtUtc",
            table: "DebtLedgerEntries");

        migrationBuilder.DropIndex(
            name: "IX_CustomerPurchaseInvoiceItems_TenantId_CustomerPurchaseInvoiceId",
            table: "CustomerPurchaseInvoiceItems");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierScrapGoldPayments_SupplierId_CreatedAtUtc",
            table: "SupplierScrapGoldPayments",
            columns: new[] { "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierScrapGoldPayments_TenantId",
            table: "SupplierScrapGoldPayments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingPayments_SupplierId_CreatedAtUtc",
            table: "SupplierManufacturingPayments",
            columns: new[] { "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingPayments_TenantId",
            table: "SupplierManufacturingPayments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingLedgerEntries_SupplierId_CreatedAtUtc",
            table: "SupplierManufacturingLedgerEntries",
            columns: new[] { "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierManufacturingLedgerEntries_TenantId",
            table: "SupplierManufacturingLedgerEntries",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierGoldLedgerEntries_SupplierId_CreatedAtUtc",
            table: "SupplierGoldLedgerEntries",
            columns: new[] { "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierGoldLedgerEntries_TenantId",
            table: "SupplierGoldLedgerEntries",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialTransactions_SupplierId_CreatedAtUtc",
            table: "SupplierFinancialTransactions",
            columns: new[] { "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialTransactions_TenantId",
            table: "SupplierFinancialTransactions",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialPayments_SupplierFinancialTransactionId_CreatedAtUtc",
            table: "SupplierFinancialPayments",
            columns: new[] { "SupplierFinancialTransactionId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialPayments_TenantId",
            table: "SupplierFinancialPayments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialLedgerEntries_SupplierFinancialTransactionId_CreatedAtUtc",
            table: "SupplierFinancialLedgerEntries",
            columns: new[] { "SupplierFinancialTransactionId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierFinancialLedgerEntries_TenantId",
            table: "SupplierFinancialLedgerEntries",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierDeliveries_SupplierId_CreatedAtUtc",
            table: "SupplierDeliveries",
            columns: new[] { "SupplierId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierDeliveries_TenantId",
            table: "SupplierDeliveries",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SalesInvoiceItems_TenantId",
            table: "SalesInvoiceItems",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_EmployeeId_PaymentDate",
            table: "SalaryPayments",
            columns: new[] { "EmployeeId", "PaymentDate" });

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_PaymentDate",
            table: "SalaryPayments",
            column: "PaymentDate");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_TenantId",
            table: "SalaryPayments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryAdjustments_CreatedAtUtc",
            table: "InventoryAdjustments",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryAdjustments_TenantId",
            table: "InventoryAdjustments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryAdjustments_Type_CreatedAtUtc",
            table: "InventoryAdjustments",
            columns: new[] { "Type", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_GoldLedgerEntries_Karat_CreatedAtUtc",
            table: "GoldLedgerEntries",
            columns: new[] { "Karat", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_GoldLedgerEntries_ReferenceType_ReferenceId",
            table: "GoldLedgerEntries",
            columns: new[] { "ReferenceType", "ReferenceId" });

        migrationBuilder.CreateIndex(
            name: "IX_GoldLedgerEntries_TenantId",
            table: "GoldLedgerEntries",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_AccountId_CreatedAtUtc",
            table: "FinancialTransactions",
            columns: new[] { "AccountId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_ReferenceType_ReferenceId",
            table: "FinancialTransactions",
            columns: new[] { "ReferenceType", "ReferenceId" });

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_TenantId",
            table: "FinancialTransactions",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_AccountId_ExpenseDate",
            table: "Expenses",
            columns: new[] { "AccountId", "ExpenseDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_ExpenseCategoryId_ExpenseDate",
            table: "Expenses",
            columns: new[] { "ExpenseCategoryId", "ExpenseDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_ExpenseDate",
            table: "Expenses",
            column: "ExpenseDate");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_TenantId",
            table: "Expenses",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Debts_CreatedAtUtc",
            table: "Debts",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Debts_Name",
            table: "Debts",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Debts_TenantId",
            table: "Debts",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_DebtLedgerEntries_DebtId_CreatedAtUtc",
            table: "DebtLedgerEntries",
            columns: new[] { "DebtId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_DebtLedgerEntries_TenantId",
            table: "DebtLedgerEntries",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_CustomerPurchaseInvoiceItems_TenantId",
            table: "CustomerPurchaseInvoiceItems",
            column: "TenantId");
    }
}

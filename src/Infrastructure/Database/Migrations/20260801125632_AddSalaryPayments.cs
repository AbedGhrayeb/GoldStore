using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddSalaryPayments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SalaryPayments",
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
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SalaryPayments_FinancialAccounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "FinancialAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_AccountId",
            table: "SalaryPayments",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_EmployeeId_PaymentDate",
            table: "SalaryPayments",
            columns: new[] { "EmployeeId", "PaymentDate" });

        migrationBuilder.CreateIndex(
            name: "IX_SalaryPayments_PaymentDate",
            table: "SalaryPayments",
            column: "PaymentDate");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SalaryPayments");
    }
}

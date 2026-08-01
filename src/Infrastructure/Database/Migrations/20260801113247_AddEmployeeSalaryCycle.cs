using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddEmployeeSalaryCycle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Title",
            table: "Employees");

        migrationBuilder.AddColumn<string>(
            name: "Role",
            table: "Employees",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<int>(
            name: "SalaryCycle",
            table: "Employees",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_Employees_UserId",
            table: "Employees",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Employees_Users_UserId",
            table: "Employees",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Employees_Users_UserId",
            table: "Employees");

        migrationBuilder.DropIndex(
            name: "IX_Employees_UserId",
            table: "Employees");

        migrationBuilder.DropColumn(
            name: "Role",
            table: "Employees");

        migrationBuilder.DropColumn(
            name: "SalaryCycle",
            table: "Employees");

        migrationBuilder.AddColumn<string>(
            name: "Title",
            table: "Employees",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "");
    }
}

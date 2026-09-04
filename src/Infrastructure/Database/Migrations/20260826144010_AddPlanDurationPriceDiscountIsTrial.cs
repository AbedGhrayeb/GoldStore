using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddPlanDurationPriceDiscountIsTrial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "DiscountPercent",
            table: "SubscriptionPlans",
            type: "decimal(5,2)",
            precision: 5,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DurationInMonths",
            table: "SubscriptionPlans",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "IsTrial",
            table: "SubscriptionPlans",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "Price",
            table: "SubscriptionPlans",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("UPDATE [SubscriptionPlans] SET [DurationInMonths]=12 WHERE [DurationInMonths]=0 AND [IsTrial]=0");
        migrationBuilder.Sql("UPDATE [SubscriptionPlans] SET [Price]=199 WHERE [Key]='standard' AND [Price]=0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DiscountPercent",
            table: "SubscriptionPlans");

        migrationBuilder.DropColumn(
            name: "DurationInMonths",
            table: "SubscriptionPlans");

        migrationBuilder.DropColumn(
            name: "IsTrial",
            table: "SubscriptionPlans");

        migrationBuilder.DropColumn(
            name: "Price",
            table: "SubscriptionPlans");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddRefreshToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Role",
            table: "Users",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ExpiresOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id)
                    .Annotation("SqlServer:Clustered", false);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_Token",
            table: "RefreshTokens",
            column: "Token",
            unique: true,
            filter: "[Token] IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "Role",
            table: "Users");
    }
}

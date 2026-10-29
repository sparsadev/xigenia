using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xigenia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMovementClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "MoneyMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE [MoneyMovements]
                SET [Classification] = 3
                WHERE [TransferGroupId] IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_MoneyMovements_Classification_Valid",
                table: "MoneyMovements",
                sql: "[Classification] IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MoneyMovements_TransferClassification_Consistent",
                table: "MoneyMovements",
                sql: "([TransferGroupId] IS NULL AND [Classification] <> 3) OR ([TransferGroupId] IS NOT NULL AND [Classification] = 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MoneyMovements_Classification_Valid",
                table: "MoneyMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MoneyMovements_TransferClassification_Consistent",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "MoneyMovements");
        }
    }
}

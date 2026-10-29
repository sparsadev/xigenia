using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xigenia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeMoneyMovementQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MoneyMovements_OwnerId",
                table: "MoneyMovements");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_Owner_MovementDate",
                table: "MoneyMovements",
                columns: new[] { "OwnerId", "MovementDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MoneyMovements_Owner_MovementDate",
                table: "MoneyMovements");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_OwnerId",
                table: "MoneyMovements",
                column: "OwnerId");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xigenia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoneyMovementFinancialCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinancialCategoryId",
                table: "MoneyMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_FinancialCategoryId",
                table: "MoneyMovements",
                column: "FinancialCategoryId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MoneyMovements_CategoryClassification_Consistent",
                table: "MoneyMovements",
                sql: "[FinancialCategoryId] IS NULL OR [Classification] IN (1, 2)");

            migrationBuilder.AddForeignKey(
                name: "FK_MoneyMovements_FinancialCategories_FinancialCategoryId",
                table: "MoneyMovements",
                column: "FinancialCategoryId",
                principalTable: "FinancialCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MoneyMovements_FinancialCategories_FinancialCategoryId",
                table: "MoneyMovements");

            migrationBuilder.DropIndex(
                name: "IX_MoneyMovements_FinancialCategoryId",
                table: "MoneyMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MoneyMovements_CategoryClassification_Consistent",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "FinancialCategoryId",
                table: "MoneyMovements");
        }
    }
}

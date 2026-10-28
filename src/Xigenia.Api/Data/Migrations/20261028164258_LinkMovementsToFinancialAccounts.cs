using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xigenia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkMovementsToFinancialAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinancialAccountId",
                table: "MoneyMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_FinancialAccountId",
                table: "MoneyMovements",
                column: "FinancialAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_MoneyMovements_FinancialAccounts_FinancialAccountId",
                table: "MoneyMovements",
                column: "FinancialAccountId",
                principalTable: "FinancialAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MoneyMovements_FinancialAccounts_FinancialAccountId",
                table: "MoneyMovements");

            migrationBuilder.DropIndex(
                name: "IX_MoneyMovements_FinancialAccountId",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "FinancialAccountId",
                table: "MoneyMovements");
        }
    }
}

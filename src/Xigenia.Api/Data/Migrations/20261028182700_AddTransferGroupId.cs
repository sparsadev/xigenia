using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xigenia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TransferGroupId",
                table: "MoneyMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_TransferGroupId",
                table: "MoneyMovements",
                column: "TransferGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MoneyMovements_TransferGroupId",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "TransferGroupId",
                table: "MoneyMovements");
        }
    }
}

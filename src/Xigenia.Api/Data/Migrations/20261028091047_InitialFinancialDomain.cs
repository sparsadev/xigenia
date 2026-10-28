using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xigenia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialFinancialDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Obligations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Total_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total_Currency = table.Column<string>(type: "varchar(3)", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obligations", x => x.Id);
                    table.CheckConstraint("CK_Obligations_DueDate_NotBeforeIssueDate", "[DueDate] IS NULL OR [DueDate] >= [IssueDate]");
                    table.CheckConstraint("CK_Obligations_Kind_Valid", "[Kind] IN (1, 2)");
                    table.CheckConstraint("CK_Obligations_TotalAmount_Positive", "[Total_Amount] > 0");
                });

            migrationBuilder.CreateTable(
                name: "MoneyMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", nullable: false),
                    MovementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoneyMovements", x => x.Id);
                    table.CheckConstraint("CK_MoneyMovements_Amount_Positive", "[Amount] > 0");
                    table.CheckConstraint("CK_MoneyMovements_Direction_Valid", "[Direction] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_MoneyMovements_Obligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "Obligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_ObligationId",
                table: "MoneyMovements",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_OwnerId",
                table: "MoneyMovements",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Obligations_OwnerId",
                table: "Obligations",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MoneyMovements");

            migrationBuilder.DropTable(
                name: "Obligations");
        }
    }
}

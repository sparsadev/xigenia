using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xigenia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StatementClosingDay = table.Column<int>(type: "int", nullable: true),
                    PaymentDueDay = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                    table.CheckConstraint("CK_FinancialAccounts_CardFields_CreditCardOnly", "[Type] = 4 OR ([CreditLimit] IS NULL AND [StatementClosingDay] IS NULL AND [PaymentDueDay] IS NULL)");
                    table.CheckConstraint("CK_FinancialAccounts_CreditLimit_Positive", "[CreditLimit] IS NULL OR [CreditLimit] > 0");
                    table.CheckConstraint("CK_FinancialAccounts_OpeningBalance_Nonnegative", "[Type] <> 4 OR [OpeningBalance] >= 0");
                    table.CheckConstraint("CK_FinancialAccounts_PaymentDueDay_Range", "[PaymentDueDay] IS NULL OR ([PaymentDueDay] >= 1 AND [PaymentDueDay] <= 31)");
                    table.CheckConstraint("CK_FinancialAccounts_StatementClosingDay_Range", "[StatementClosingDay] IS NULL OR ([StatementClosingDay] >= 1 AND [StatementClosingDay] <= 31)");
                    table.CheckConstraint("CK_FinancialAccounts_Type_Valid", "[Type] IN (1, 2, 3, 4)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_OwnerId",
                table: "FinancialAccounts",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialAccounts");
        }
    }
}

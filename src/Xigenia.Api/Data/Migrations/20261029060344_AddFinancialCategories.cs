using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xigenia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialCategories", x => x.Id);
                    table.CheckConstraint("CK_FinancialCategories_Classification_Valid", "[Classification] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_FinancialCategories_FinancialCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "FinancialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategories_Classification",
                table: "FinancialCategories",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategories_OwnerId",
                table: "FinancialCategories",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategories_ParentCategoryId",
                table: "FinancialCategories",
                column: "ParentCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialCategories");
        }
    }
}

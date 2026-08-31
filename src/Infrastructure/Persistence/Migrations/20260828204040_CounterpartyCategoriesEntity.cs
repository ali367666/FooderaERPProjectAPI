using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CounterpartyCategoriesEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CategoryType",
                table: "Counterparties",
                newName: "CategoryId");

            migrationBuilder.CreateTable(
                name: "CounterpartyCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterpartyCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CounterpartyCategories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_CategoryId",
                table: "Counterparties",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CounterpartyCategories_CompanyId_Name",
                table: "CounterpartyCategories",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Counterparties_CounterpartyCategories_CategoryId",
                table: "Counterparties",
                column: "CategoryId",
                principalTable: "CounterpartyCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Counterparties_CounterpartyCategories_CategoryId",
                table: "Counterparties");

            migrationBuilder.DropTable(
                name: "CounterpartyCategories");

            migrationBuilder.DropIndex(
                name: "IX_Counterparties_CategoryId",
                table: "Counterparties");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Counterparties",
                newName: "CategoryType");
        }
    }
}

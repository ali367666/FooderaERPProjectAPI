using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Drop_MenuCategoryPrinterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuCategories_Printers_PrinterId",
                table: "MenuCategories");

            migrationBuilder.DropIndex(
                name: "IX_MenuCategories_PrinterId",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "PrinterId",
                table: "MenuCategories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrinterId",
                table: "MenuCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_PrinterId",
                table: "MenuCategories",
                column: "PrinterId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuCategories_Printers_PrinterId",
                table: "MenuCategories",
                column: "PrinterId",
                principalTable: "Printers",
                principalColumn: "Id");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MenuItemStockItemLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockItemId",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_StockItemId",
                table: "MenuItems",
                column: "StockItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_StockItems_StockItemId",
                table: "MenuItems",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_StockItems_StockItemId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_StockItemId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "StockItemId",
                table: "MenuItems");
        }
    }
}

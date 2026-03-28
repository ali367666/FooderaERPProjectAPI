using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedStockItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinLevel",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "QuantityOnHand",
                table: "StockItems");

            migrationBuilder.AddColumn<int>(
                name: "StockItemId1",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_StockItemId1",
                table: "WarehouseStocks",
                column: "StockItemId1");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseStocks_StockItems_StockItemId1",
                table: "WarehouseStocks",
                column: "StockItemId1",
                principalTable: "StockItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseStocks_StockItems_StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.AddColumn<decimal>(
                name: "MinLevel",
                table: "StockItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityOnHand",
                table: "StockItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}

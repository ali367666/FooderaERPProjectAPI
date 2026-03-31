using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedWarehouseStockConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_Restaurants_RestaurantId",
                table: "StockItems");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_CompanyId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_StockItemId_WarehouseId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_RestaurantId",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "StockItems");

            migrationBuilder.AddColumn<int>(
                name: "StockItemId1",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId1",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_CompanyId_WarehouseId_StockItemId",
                table: "WarehouseStocks",
                columns: new[] { "CompanyId", "WarehouseId", "StockItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_StockItemId",
                table: "WarehouseStocks",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_StockItemId1",
                table: "WarehouseStocks",
                column: "StockItemId1");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_WarehouseId1",
                table: "WarehouseStocks",
                column: "WarehouseId1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WarehouseStocks_QuantityOnHand",
                table: "WarehouseStocks",
                sql: "[QuantityOnHand] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WarehouseStocks_MinLevel",
                table: "WarehouseStocks",
                sql: "[MinLevel] IS NULL OR [MinLevel] >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseStocks_StockItems_StockItemId1",
                table: "WarehouseStocks",
                column: "StockItemId1",
                principalTable: "StockItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseStocks_Warehouses_WarehouseId1",
                table: "WarehouseStocks",
                column: "WarehouseId1",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseStocks_StockItems_StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseStocks_Warehouses_WarehouseId1",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_CompanyId_WarehouseId_StockItemId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_StockItemId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_WarehouseId1",
                table: "WarehouseStocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WarehouseStocks_QuantityOnHand",
                table: "WarehouseStocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WarehouseStocks_MinLevel",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "WarehouseId1",
                table: "WarehouseStocks");

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "StockItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_CompanyId",
                table: "WarehouseStocks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_StockItemId_WarehouseId",
                table: "WarehouseStocks",
                columns: new[] { "StockItemId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_RestaurantId",
                table: "StockItems",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_Restaurants_RestaurantId",
                table: "StockItems",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

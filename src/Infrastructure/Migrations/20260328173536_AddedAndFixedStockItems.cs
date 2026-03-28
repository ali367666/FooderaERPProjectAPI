using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedAndFixedStockItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseStocks_Companies_CompanyId",
                table: "WarehouseStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseStocks_StockItems_StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_StockItemId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_WarehouseId_StockItemId",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "StockItemId1",
                table: "WarehouseStocks");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_StockItemId_WarehouseId",
                table: "WarehouseStocks",
                columns: new[] { "StockItemId", "WarehouseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseStocks_Companies_CompanyId",
                table: "WarehouseStocks",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseStocks_Companies_CompanyId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_StockItemId_WarehouseId",
                table: "WarehouseStocks");

            migrationBuilder.AddColumn<int>(
                name: "StockItemId1",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_StockItemId",
                table: "WarehouseStocks",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_StockItemId1",
                table: "WarehouseStocks",
                column: "StockItemId1");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_WarehouseId_StockItemId",
                table: "WarehouseStocks",
                columns: new[] { "WarehouseId", "StockItemId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseStocks_Companies_CompanyId",
                table: "WarehouseStocks",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseStocks_StockItems_StockItemId1",
                table: "WarehouseStocks",
                column: "StockItemId1",
                principalTable: "StockItems",
                principalColumn: "Id");
        }
    }
}

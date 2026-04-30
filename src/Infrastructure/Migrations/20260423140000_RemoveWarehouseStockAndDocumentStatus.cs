using System;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260423140000_RemoveWarehouseStockAndDocumentStatus")]
    public class RemoveWarehouseStockAndDocumentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO StockMovements (CompanyId, CreatedAtUtc, Note, Quantity, StockItemId, Type, WarehouseId, WarehouseTransferId, CreatedByUserId, LastModifiedAtUtc, LastModifiedByUserId)
                SELECT CompanyId, GETUTCDATE(), N'Opening balance (migrated from WarehouseStocks)', QuantityOnHand, StockItemId, 2, WarehouseId, NULL, NULL, NULL, NULL
                FROM WarehouseStocks
                WHERE QuantityOnHand > 0
                ");

            migrationBuilder.DropTable(
                name: "WarehouseStocks");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WarehouseStockDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "WarehouseStockLines",
                newName: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitId",
                table: "WarehouseStockLines",
                newName: "Unit");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WarehouseStockDocuments");

            migrationBuilder.CreateTable(
                name: "WarehouseStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinLevel = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "IX_WarehouseStocks_WarehouseId",
                table: "WarehouseStocks",
                column: "WarehouseId");
        }
    }
}

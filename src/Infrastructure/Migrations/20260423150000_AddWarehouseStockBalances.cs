using System;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <summary>Recreates WarehouseStocks as the current balance table (one row per company, warehouse, stock item).</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260423150000_AddWarehouseStockBalances")]
public class AddWarehouseStockBalances : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WarehouseStocks",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(type: "int", nullable: false),
                WarehouseId = table.Column<int>(type: "int", nullable: false),
                StockItemId = table.Column<int>(type: "int", nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                UnitId = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WarehouseStocks", x => x.Id);
                table.ForeignKey(
                    name: "FK_WarehouseStocks_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
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
            name: "IX_WarehouseStocks_CompanyId",
            table: "WarehouseStocks",
            column: "CompanyId");

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

        migrationBuilder.Sql(@"
INSERT INTO WarehouseStocks (CompanyId, WarehouseId, StockItemId, Quantity, UnitId, CreatedAtUtc, CreatedByUserId, LastModifiedAtUtc, LastModifiedByUserId)
SELECT a.CompanyId, a.WarehouseId, a.StockItemId, a.NetQty, si.Unit, GETUTCDATE(), NULL, NULL, NULL
FROM (
    SELECT CompanyId, WarehouseId, StockItemId,
        SUM(CASE WHEN [Type] = 2 THEN Quantity WHEN [Type] = 1 THEN -Quantity ELSE 0 END) AS NetQty
    FROM StockMovements
    GROUP BY CompanyId, WarehouseId, StockItemId
) a
INNER JOIN StockItems si ON si.Id = a.StockItemId
WHERE a.NetQty <> 0
  AND NOT EXISTS (
    SELECT 1 FROM WarehouseStocks ws
    WHERE ws.CompanyId = a.CompanyId AND ws.WarehouseId = a.WarehouseId AND ws.StockItemId = a.StockItemId);
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "WarehouseStocks");
    }
}

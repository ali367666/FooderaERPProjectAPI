using System;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260423120000_AddWarehouseStockDocuments")]
    public class AddWarehouseStockDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseStockDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStockDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStockDocuments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStockDocuments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseStockLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseStockDocumentId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStockLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStockLines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStockLines_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStockLines_WarehouseStockDocuments_WarehouseStockDocumentId",
                        column: x => x.WarehouseStockDocumentId,
                        principalTable: "WarehouseStockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockDocuments_CompanyId",
                table: "WarehouseStockDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockDocuments_WarehouseId",
                table: "WarehouseStockDocuments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockDocuments_CompanyId_DocumentNo",
                table: "WarehouseStockDocuments",
                columns: new[] { "CompanyId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockLines_CompanyId",
                table: "WarehouseStockLines",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockLines_StockItemId",
                table: "WarehouseStockLines",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockLines_WarehouseStockDocumentId",
                table: "WarehouseStockLines",
                column: "WarehouseStockDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockLines_WarehouseStockDocumentId_StockItemId",
                table: "WarehouseStockLines",
                columns: new[] { "WarehouseStockDocumentId", "StockItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseStockLines");

            migrationBuilder.DropTable(
                name: "WarehouseStockDocuments");
        }
    }
}

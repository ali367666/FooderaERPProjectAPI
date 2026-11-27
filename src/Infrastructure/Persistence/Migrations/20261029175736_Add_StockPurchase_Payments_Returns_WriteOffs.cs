using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StockPurchase_Payments_Returns_WriteOffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockPurchasePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockPurchaseId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RemainingAfterAzn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPurchasePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPurchasePayments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPurchasePayments_StockPurchases_StockPurchaseId",
                        column: x => x.StockPurchaseId,
                        principalTable: "StockPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockPurchaseReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockPurchaseId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPurchaseReturns_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPurchaseReturns_StockPurchases_StockPurchaseId",
                        column: x => x.StockPurchaseId,
                        principalTable: "StockPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockWriteOffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCostAzn = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StockPurchaseId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockWriteOffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_StockPurchases_StockPurchaseId",
                        column: x => x.StockPurchaseId,
                        principalTable: "StockPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockPurchaseReturnLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockPurchaseReturnId = table.Column<int>(type: "int", nullable: false),
                    StockPurchaseLineId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPriceAzn = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPurchaseReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPurchaseReturnLines_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPurchaseReturnLines_StockPurchaseLines_StockPurchaseLineId",
                        column: x => x.StockPurchaseLineId,
                        principalTable: "StockPurchaseLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPurchaseReturnLines_StockPurchaseReturns_StockPurchaseReturnId",
                        column: x => x.StockPurchaseReturnId,
                        principalTable: "StockPurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchasePayments_CompanyId",
                table: "StockPurchasePayments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchasePayments_StockPurchaseId",
                table: "StockPurchasePayments",
                column: "StockPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseReturnLines_StockItemId",
                table: "StockPurchaseReturnLines",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseReturnLines_StockPurchaseLineId",
                table: "StockPurchaseReturnLines",
                column: "StockPurchaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseReturnLines_StockPurchaseReturnId",
                table: "StockPurchaseReturnLines",
                column: "StockPurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseReturns_CompanyId",
                table: "StockPurchaseReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseReturns_StockPurchaseId",
                table: "StockPurchaseReturns",
                column: "StockPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_CompanyId_CreatedAtUtc",
                table: "StockWriteOffs",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_StockItemId",
                table: "StockWriteOffs",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_StockPurchaseId",
                table: "StockWriteOffs",
                column: "StockPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_WarehouseId",
                table: "StockWriteOffs",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockPurchasePayments");

            migrationBuilder.DropTable(
                name: "StockPurchaseReturnLines");

            migrationBuilder.DropTable(
                name: "StockWriteOffs");

            migrationBuilder.DropTable(
                name: "StockPurchaseReturns");
        }
    }
}

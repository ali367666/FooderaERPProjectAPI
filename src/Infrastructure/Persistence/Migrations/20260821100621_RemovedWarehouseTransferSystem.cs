using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovedWarehouseTransferSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_WarehouseTransfers_WarehouseTransferId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "WarehouseTransferLines");

            migrationBuilder.DropTable(
                name: "WarehouseTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_WarehouseTransferId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "WarehouseTransferId",
                table: "StockMovements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarehouseTransferId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarehouseTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FromWarehouseId = table.Column<int>(type: "int", nullable: false),
                    StockRequestId = table.Column<int>(type: "int", nullable: true),
                    ToWarehouseId = table.Column<int>(type: "int", nullable: false),
                    VehicleWarehouseId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_StockRequests_StockRequestId",
                        column: x => x.StockRequestId,
                        principalTable: "StockRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_VehicleWarehouseId",
                        column: x => x.VehicleWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransferLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    WarehouseTransferId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransferLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_WarehouseTransfers_WarehouseTransferId",
                        column: x => x.WarehouseTransferId,
                        principalTable: "WarehouseTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseTransferId",
                table: "StockMovements",
                column: "WarehouseTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_CompanyId",
                table: "WarehouseTransferLines",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_StockItemId",
                table: "WarehouseTransferLines",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_WarehouseTransferId",
                table: "WarehouseTransferLines",
                column: "WarehouseTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_WarehouseTransferId_StockItemId",
                table: "WarehouseTransferLines",
                columns: new[] { "WarehouseTransferId", "StockItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_CompanyId",
                table: "WarehouseTransfers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_CompanyId_DocumentNo",
                table: "WarehouseTransfers",
                columns: new[] { "CompanyId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_FromWarehouseId",
                table: "WarehouseTransfers",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_Status",
                table: "WarehouseTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_StockRequestId",
                table: "WarehouseTransfers",
                column: "StockRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_ToWarehouseId",
                table: "WarehouseTransfers",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_VehicleWarehouseId",
                table: "WarehouseTransfers",
                column: "VehicleWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_WarehouseTransfers_WarehouseTransferId",
                table: "StockMovements",
                column: "WarehouseTransferId",
                principalTable: "WarehouseTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

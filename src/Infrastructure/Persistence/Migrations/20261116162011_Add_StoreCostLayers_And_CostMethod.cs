using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StoreCostLayers_And_CostMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreCostMethod",
                table: "CompanySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StoreCostLayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StoreProductId = table.Column<int>(type: "int", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsOpeningBalance = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreCostLayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreCostLayers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreCostLayers_StoreProducts_StoreProductId",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreCostLayers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreCostLayers_CompanyId_WarehouseId_StoreProductId_ReceivedAtUtc",
                table: "StoreCostLayers",
                columns: new[] { "CompanyId", "WarehouseId", "StoreProductId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreCostLayers_StoreProductId",
                table: "StoreCostLayers",
                column: "StoreProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCostLayers_WarehouseId",
                table: "StoreCostLayers",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreCostLayers");

            migrationBuilder.DropColumn(
                name: "StoreCostMethod",
                table: "CompanySettings");
        }
    }
}

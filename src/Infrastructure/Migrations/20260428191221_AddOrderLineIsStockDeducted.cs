using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLineIsStockDeducted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStockDeducted",
                table: "OrderLines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MenuItemRecipeLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    QuantityPerPortion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemRecipeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemRecipeLines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuItemRecipeLines_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuItemRecipeLines_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRecipeLines_CompanyId_MenuItemId_StockItemId",
                table: "MenuItemRecipeLines",
                columns: new[] { "CompanyId", "MenuItemId", "StockItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRecipeLines_MenuItemId",
                table: "MenuItemRecipeLines",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRecipeLines_StockItemId",
                table: "MenuItemRecipeLines",
                column: "StockItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItemRecipeLines");

            migrationBuilder.DropColumn(
                name: "IsStockDeducted",
                table: "OrderLines");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Counterparties_MenuItemsExpansion_SetBundles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CompanyId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "StockPurchases");

            migrationBuilder.AddColumn<int>(
                name: "CounterpartyId",
                table: "StockPurchases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentLineId",
                table: "OrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowQuantityPromptOverride",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "MenuItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeFromDiscount",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HideBarcode",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HideFromPosSearch",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSet",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTimeBased",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PackagePrice",
                table: "MenuItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrinterId",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "MenuItems",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SkipTaxCalculation",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialPrice1",
                table: "MenuItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialPrice2",
                table: "MenuItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialPrice3",
                table: "MenuItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialPrice4",
                table: "MenuItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialPrice5",
                table: "MenuItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StationPrice",
                table: "MenuItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "VatPercent",
                table: "MenuItems",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeightCode",
                table: "MenuItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryId",
                table: "MenuCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Counterparties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CategoryType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CurrentDebtAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counterparties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Counterparties_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemSetComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SetMenuItemId = table.Column<int>(type: "int", nullable: false),
                    ComponentMenuItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemSetComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemSetComponents_MenuItems_ComponentMenuItemId",
                        column: x => x.ComponentMenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItemSetComponents_MenuItems_SetMenuItemId",
                        column: x => x.SetMenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchases_CounterpartyId",
                table: "StockPurchases",
                column: "CounterpartyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ParentLineId",
                table: "OrderLines",
                column: "ParentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CompanyId_Barcode",
                table: "MenuItems",
                columns: new[] { "CompanyId", "Barcode" },
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CompanyId_WeightCode",
                table: "MenuItems",
                columns: new[] { "CompanyId", "WeightCode" },
                unique: true,
                filter: "[WeightCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_PrinterId",
                table: "MenuItems",
                column: "PrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_ParentCategoryId",
                table: "MenuCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_CompanyId_Name",
                table: "Counterparties",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemSetComponents_ComponentMenuItemId",
                table: "MenuItemSetComponents",
                column: "ComponentMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemSetComponents_SetMenuItemId_ComponentMenuItemId",
                table: "MenuItemSetComponents",
                columns: new[] { "SetMenuItemId", "ComponentMenuItemId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuCategories_MenuCategories_ParentCategoryId",
                table: "MenuCategories",
                column: "ParentCategoryId",
                principalTable: "MenuCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Printers_PrinterId",
                table: "MenuItems",
                column: "PrinterId",
                principalTable: "Printers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_OrderLines_ParentLineId",
                table: "OrderLines",
                column: "ParentLineId",
                principalTable: "OrderLines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockPurchases_Counterparties_CounterpartyId",
                table: "StockPurchases",
                column: "CounterpartyId",
                principalTable: "Counterparties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuCategories_MenuCategories_ParentCategoryId",
                table: "MenuCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Printers_PrinterId",
                table: "MenuItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_OrderLines_ParentLineId",
                table: "OrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_StockPurchases_Counterparties_CounterpartyId",
                table: "StockPurchases");

            migrationBuilder.DropTable(
                name: "Counterparties");

            migrationBuilder.DropTable(
                name: "MenuItemSetComponents");

            migrationBuilder.DropIndex(
                name: "IX_StockPurchases_CounterpartyId",
                table: "StockPurchases");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_ParentLineId",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CompanyId_Barcode",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CompanyId_WeightCode",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_PrinterId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuCategories_ParentCategoryId",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "CounterpartyId",
                table: "StockPurchases");

            migrationBuilder.DropColumn(
                name: "ParentLineId",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "AllowQuantityPromptOverride",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ExcludeFromDiscount",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "HideBarcode",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "HideFromPosSearch",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsSet",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsTimeBased",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "PackagePrice",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "PrinterId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SkipTaxCalculation",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SpecialPrice1",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SpecialPrice2",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SpecialPrice3",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SpecialPrice4",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SpecialPrice5",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "StationPrice",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "VatPercent",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "WeightCode",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "MenuCategories");

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "StockPurchases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CompanyId",
                table: "MenuItems",
                column: "CompanyId");
        }
    }
}

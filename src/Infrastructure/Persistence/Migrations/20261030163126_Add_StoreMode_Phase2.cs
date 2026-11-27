using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StoreMode_Phase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountPercent",
                table: "StoreSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "ListPrice",
                table: "StoreSaleLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PriceChangeReason",
                table: "StoreSaleLines",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreApprovalLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreApprovalLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreApprovalLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreHeldSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreCashRegisterId = table.Column<int>(type: "int", nullable: false),
                    CashierUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CartJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreHeldSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreHeldSales_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreHeldSales_StoreCashRegisters_StoreCashRegisterId",
                        column: x => x.StoreCashRegisterId,
                        principalTable: "StoreCashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreQuickSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreCashRegisterId = table.Column<int>(type: "int", nullable: false),
                    SlotNumber = table.Column<int>(type: "int", nullable: false),
                    StoreProductId = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreQuickSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreQuickSlots_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreQuickSlots_StoreCashRegisters_StoreCashRegisterId",
                        column: x => x.StoreCashRegisterId,
                        principalTable: "StoreCashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreQuickSlots_StoreProducts_StoreProductId",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreSaleReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreSaleId = table.Column<int>(type: "int", nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StoreCashRegisterId = table.Column<int>(type: "int", nullable: false),
                    StoreCashShiftId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundMethod = table.Column<int>(type: "int", nullable: false),
                    ExchangeSaleId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSaleReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSaleReturns_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreSaleReturns_StoreCashRegisters_StoreCashRegisterId",
                        column: x => x.StoreCashRegisterId,
                        principalTable: "StoreCashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreSaleReturns_StoreCashShifts_StoreCashShiftId",
                        column: x => x.StoreCashShiftId,
                        principalTable: "StoreCashShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreSaleReturns_StoreSales_StoreSaleId",
                        column: x => x.StoreSaleId,
                        principalTable: "StoreSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreSerialUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreProductId = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StoreSaleLineId = table.Column<int>(type: "int", nullable: true),
                    SoldAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSerialUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSerialUnits_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreSerialUnits_StoreProducts_StoreProductId",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreSerialUnits_StoreSaleLines_StoreSaleLineId",
                        column: x => x.StoreSaleLineId,
                        principalTable: "StoreSaleLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreSerialUnits_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreSaleReturnLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreSaleReturnId = table.Column<int>(type: "int", nullable: false),
                    StoreSaleLineId = table.Column<int>(type: "int", nullable: false),
                    StoreProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSaleReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSaleReturnLines_StoreSaleLines_StoreSaleLineId",
                        column: x => x.StoreSaleLineId,
                        principalTable: "StoreSaleLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreSaleReturnLines_StoreSaleReturns_StoreSaleReturnId",
                        column: x => x.StoreSaleReturnId,
                        principalTable: "StoreSaleReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreSerialEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreSerialUnitId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSerialEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSerialEvents_StoreSerialUnits_StoreSerialUnitId",
                        column: x => x.StoreSerialUnitId,
                        principalTable: "StoreSerialUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreApprovalLogs_CompanyId",
                table: "StoreApprovalLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreHeldSales_CompanyId",
                table: "StoreHeldSales",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreHeldSales_StoreCashRegisterId",
                table: "StoreHeldSales",
                column: "StoreCashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreQuickSlots_CompanyId",
                table: "StoreQuickSlots",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreQuickSlots_StoreCashRegisterId_SlotNumber",
                table: "StoreQuickSlots",
                columns: new[] { "StoreCashRegisterId", "SlotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreQuickSlots_StoreProductId",
                table: "StoreQuickSlots",
                column: "StoreProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaleReturnLines_StoreSaleLineId",
                table: "StoreSaleReturnLines",
                column: "StoreSaleLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaleReturnLines_StoreSaleReturnId",
                table: "StoreSaleReturnLines",
                column: "StoreSaleReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaleReturns_CompanyId_DocumentNo",
                table: "StoreSaleReturns",
                columns: new[] { "CompanyId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaleReturns_CompanyId_OperationId",
                table: "StoreSaleReturns",
                columns: new[] { "CompanyId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaleReturns_StoreCashRegisterId",
                table: "StoreSaleReturns",
                column: "StoreCashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaleReturns_StoreCashShiftId",
                table: "StoreSaleReturns",
                column: "StoreCashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSaleReturns_StoreSaleId",
                table: "StoreSaleReturns",
                column: "StoreSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSerialEvents_StoreSerialUnitId",
                table: "StoreSerialEvents",
                column: "StoreSerialUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSerialUnits_CompanyId_SerialNumber",
                table: "StoreSerialUnits",
                columns: new[] { "CompanyId", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSerialUnits_StoreProductId_WarehouseId_Status",
                table: "StoreSerialUnits",
                columns: new[] { "StoreProductId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreSerialUnits_StoreSaleLineId",
                table: "StoreSerialUnits",
                column: "StoreSaleLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSerialUnits_WarehouseId",
                table: "StoreSerialUnits",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreApprovalLogs");

            migrationBuilder.DropTable(
                name: "StoreHeldSales");

            migrationBuilder.DropTable(
                name: "StoreQuickSlots");

            migrationBuilder.DropTable(
                name: "StoreSaleReturnLines");

            migrationBuilder.DropTable(
                name: "StoreSerialEvents");

            migrationBuilder.DropTable(
                name: "StoreSaleReturns");

            migrationBuilder.DropTable(
                name: "StoreSerialUnits");

            migrationBuilder.DropColumn(
                name: "MaxDiscountPercent",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "ListPrice",
                table: "StoreSaleLines");

            migrationBuilder.DropColumn(
                name: "PriceChangeReason",
                table: "StoreSaleLines");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StoreMode_Phase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartyType = table.Column<int>(type: "int", nullable: false),
                    PartyId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreLedgerEntries_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StorePriceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreProductId = table.Column<int>(type: "int", nullable: false),
                    PriceType = table.Column<int>(type: "int", nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    NewPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorePriceHistories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorePriceHistories_StoreProducts_StoreProductId",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreSuppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSuppliers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StorePurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StoreSupplierId = table.Column<int>(type: "int", nullable: false),
                    SupplierInvoiceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorePurchases_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorePurchases_StoreSuppliers_StoreSupplierId",
                        column: x => x.StoreSupplierId,
                        principalTable: "StoreSuppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorePurchases_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreSupplierPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StoreSupplierId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSupplierPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSupplierPayments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreSupplierPayments_StoreSuppliers_StoreSupplierId",
                        column: x => x.StoreSupplierId,
                        principalTable: "StoreSuppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StorePurchaseLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StorePurchaseId = table.Column<int>(type: "int", nullable: false),
                    StoreProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VatPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetUnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SerialNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePurchaseLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorePurchaseLines_StoreProducts_StoreProductId",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorePurchaseLines_StorePurchases_StorePurchaseId",
                        column: x => x.StorePurchaseId,
                        principalTable: "StorePurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorePurchaseReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StorePurchaseId = table.Column<int>(type: "int", nullable: false),
                    StoreSupplierId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorePurchaseReturns_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorePurchaseReturns_StorePurchases_StorePurchaseId",
                        column: x => x.StorePurchaseId,
                        principalTable: "StorePurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StorePaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreSupplierPaymentId = table.Column<int>(type: "int", nullable: false),
                    StorePurchaseId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorePaymentAllocations_StorePurchases_StorePurchaseId",
                        column: x => x.StorePurchaseId,
                        principalTable: "StorePurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorePaymentAllocations_StoreSupplierPayments_StoreSupplierPaymentId",
                        column: x => x.StoreSupplierPaymentId,
                        principalTable: "StoreSupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorePurchaseReturnLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StorePurchaseReturnId = table.Column<int>(type: "int", nullable: false),
                    StorePurchaseLineId = table.Column<int>(type: "int", nullable: false),
                    StoreProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePurchaseReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorePurchaseReturnLines_StorePurchaseLines_StorePurchaseLineId",
                        column: x => x.StorePurchaseLineId,
                        principalTable: "StorePurchaseLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorePurchaseReturnLines_StorePurchaseReturns_StorePurchaseReturnId",
                        column: x => x.StorePurchaseReturnId,
                        principalTable: "StorePurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreLedgerEntries_CompanyId_PartyType_PartyId_EntryDate",
                table: "StoreLedgerEntries",
                columns: new[] { "CompanyId", "PartyType", "PartyId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StorePaymentAllocations_StorePurchaseId",
                table: "StorePaymentAllocations",
                column: "StorePurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePaymentAllocations_StoreSupplierPaymentId",
                table: "StorePaymentAllocations",
                column: "StoreSupplierPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePriceHistories_CompanyId",
                table: "StorePriceHistories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePriceHistories_StoreProductId_EffectiveFromUtc",
                table: "StorePriceHistories",
                columns: new[] { "StoreProductId", "EffectiveFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchaseLines_StoreProductId",
                table: "StorePurchaseLines",
                column: "StoreProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchaseLines_StorePurchaseId",
                table: "StorePurchaseLines",
                column: "StorePurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchaseReturnLines_StorePurchaseLineId",
                table: "StorePurchaseReturnLines",
                column: "StorePurchaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchaseReturnLines_StorePurchaseReturnId",
                table: "StorePurchaseReturnLines",
                column: "StorePurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchaseReturns_CompanyId_DocumentNo",
                table: "StorePurchaseReturns",
                columns: new[] { "CompanyId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchaseReturns_StorePurchaseId",
                table: "StorePurchaseReturns",
                column: "StorePurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchases_CompanyId_DocumentNo",
                table: "StorePurchases",
                columns: new[] { "CompanyId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchases_StoreSupplierId_Status",
                table: "StorePurchases",
                columns: new[] { "StoreSupplierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchases_WarehouseId",
                table: "StorePurchases",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSupplierPayments_CompanyId_DocumentNo",
                table: "StoreSupplierPayments",
                columns: new[] { "CompanyId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSupplierPayments_StoreSupplierId",
                table: "StoreSupplierPayments",
                column: "StoreSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSuppliers_CompanyId_Name",
                table: "StoreSuppliers",
                columns: new[] { "CompanyId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreLedgerEntries");

            migrationBuilder.DropTable(
                name: "StorePaymentAllocations");

            migrationBuilder.DropTable(
                name: "StorePriceHistories");

            migrationBuilder.DropTable(
                name: "StorePurchaseReturnLines");

            migrationBuilder.DropTable(
                name: "StoreSupplierPayments");

            migrationBuilder.DropTable(
                name: "StorePurchaseLines");

            migrationBuilder.DropTable(
                name: "StorePurchaseReturns");

            migrationBuilder.DropTable(
                name: "StorePurchases");

            migrationBuilder.DropTable(
                name: "StoreSuppliers");
        }
    }
}

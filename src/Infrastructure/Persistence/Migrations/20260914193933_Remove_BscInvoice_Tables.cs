using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Remove_BscInvoice_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BscInvoiceDs");

            migrationBuilder.DropTable(
                name: "BscInvoiceMs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BscInvoiceMs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amt = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmtVat = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    BscCreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BscInvoiceMId = table.Column<int>(type: "int", nullable: false),
                    CoId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DocDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    PurchaseSales = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BscInvoiceMs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BscInvoiceDs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BscInvoiceMId = table.Column<int>(type: "int", nullable: false),
                    Amt = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AmtVat = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    BscCreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BscInvoiceDId = table.Column<int>(type: "int", nullable: false),
                    CoId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DocDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BscInvoiceDs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BscInvoiceDs_BscInvoiceMs_BscInvoiceMId",
                        column: x => x.BscInvoiceMId,
                        principalTable: "BscInvoiceMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BscInvoiceDs_BscInvoiceDId",
                table: "BscInvoiceDs",
                column: "BscInvoiceDId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BscInvoiceDs_BscInvoiceMId",
                table: "BscInvoiceDs",
                column: "BscInvoiceMId");

            migrationBuilder.CreateIndex(
                name: "IX_BscInvoiceMs_BscInvoiceMId",
                table: "BscInvoiceMs",
                column: "BscInvoiceMId",
                unique: true);
        }
    }
}

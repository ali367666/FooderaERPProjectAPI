using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StoreMode_Phase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BonusEarnPercent",
                table: "StoreSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BonusMaxPaymentPercent",
                table: "StoreSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 50m);

            migrationBuilder.AddColumn<decimal>(
                name: "BonusEarned",
                table: "StoreSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BonusSpent",
                table: "StoreSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmount",
                table: "StoreSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StoreCustomerId",
                table: "StoreSales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BonusClawbackAmount",
                table: "StoreSaleReturns",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BonusRefundAmount",
                table: "StoreSaleReturns",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtCreditAmount",
                table: "StoreSaleReturns",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "StoreCustomers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreCustomers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreCustomers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreBonusEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreCustomerId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_StoreBonusEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreBonusEntries_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreBonusEntries_StoreCustomers_StoreCustomerId",
                        column: x => x.StoreCustomerId,
                        principalTable: "StoreCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreCustomerPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StoreCustomerId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    StoreCashShiftId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StoreCustomerPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreCustomerPayments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreCustomerPayments_StoreCashShifts_StoreCashShiftId",
                        column: x => x.StoreCashShiftId,
                        principalTable: "StoreCashShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreCustomerPayments_StoreCustomers_StoreCustomerId",
                        column: x => x.StoreCustomerId,
                        principalTable: "StoreCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreSales_StoreCustomerId",
                table: "StoreSales",
                column: "StoreCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreBonusEntries_CompanyId",
                table: "StoreBonusEntries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreBonusEntries_StoreCustomerId_CreatedAtUtc",
                table: "StoreBonusEntries",
                columns: new[] { "StoreCustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreCustomerPayments_CompanyId_DocumentNo",
                table: "StoreCustomerPayments",
                columns: new[] { "CompanyId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreCustomerPayments_StoreCashShiftId",
                table: "StoreCustomerPayments",
                column: "StoreCashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCustomerPayments_StoreCustomerId",
                table: "StoreCustomerPayments",
                column: "StoreCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCustomers_CompanyId_Code",
                table: "StoreCustomers",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreCustomers_CompanyId_Phone",
                table: "StoreCustomers",
                columns: new[] { "CompanyId", "Phone" });

            migrationBuilder.AddForeignKey(
                name: "FK_StoreSales_StoreCustomers_StoreCustomerId",
                table: "StoreSales",
                column: "StoreCustomerId",
                principalTable: "StoreCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreSales_StoreCustomers_StoreCustomerId",
                table: "StoreSales");

            migrationBuilder.DropTable(
                name: "StoreBonusEntries");

            migrationBuilder.DropTable(
                name: "StoreCustomerPayments");

            migrationBuilder.DropTable(
                name: "StoreCustomers");

            migrationBuilder.DropIndex(
                name: "IX_StoreSales_StoreCustomerId",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "BonusEarnPercent",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "BonusMaxPaymentPercent",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "BonusEarned",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "BonusSpent",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "DebtAmount",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "StoreCustomerId",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "BonusClawbackAmount",
                table: "StoreSaleReturns");

            migrationBuilder.DropColumn(
                name: "BonusRefundAmount",
                table: "StoreSaleReturns");

            migrationBuilder.DropColumn(
                name: "DebtCreditAmount",
                table: "StoreSaleReturns");
        }
    }
}

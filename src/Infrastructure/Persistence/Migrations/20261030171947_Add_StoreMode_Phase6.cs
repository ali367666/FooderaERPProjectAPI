using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StoreMode_Phase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BonusExpiryDays",
                table: "StoreSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FiscalEnabled",
                table: "StoreSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FiscalError",
                table: "StoreSales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalReceiptNo",
                table: "StoreSales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalStatus",
                table: "StoreSales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EarnsBonus",
                table: "StoreProducts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "StoreSyncOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    ClientCreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultDocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResultEntityId = table.Column<int>(type: "int", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSyncOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSyncOperations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreSyncOperations_CompanyId_OperationId",
                table: "StoreSyncOperations",
                columns: new[] { "CompanyId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSyncOperations_CompanyId_Status",
                table: "StoreSyncOperations",
                columns: new[] { "CompanyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreSyncOperations");

            migrationBuilder.DropColumn(
                name: "BonusExpiryDays",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "FiscalEnabled",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "FiscalError",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "FiscalReceiptNo",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "FiscalStatus",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "EarnsBonus",
                table: "StoreProducts");
        }
    }
}

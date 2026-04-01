using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StockRequestLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransfers_StockRequests_StockRequestId",
                table: "WarehouseTransfers");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleWarehouseId",
                table: "WarehouseTransfers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "WarehouseTransfers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransferDate",
                table: "WarehouseTransfers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "StockRequests",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequestLines_StockRequestId_StockItemId",
                table: "StockRequestLines",
                columns: new[] { "StockRequestId", "StockItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CompanyId_WarehouseId_StockItemId",
                table: "StockMovements",
                columns: new[] { "CompanyId", "WarehouseId", "StockItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Type",
                table: "StockMovements",
                column: "Type");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransfers_StockRequests_StockRequestId",
                table: "WarehouseTransfers",
                column: "StockRequestId",
                principalTable: "StockRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransfers_StockRequests_StockRequestId",
                table: "WarehouseTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockRequestLines_StockRequestId_StockItemId",
                table: "StockRequestLines");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CompanyId_WarehouseId_StockItemId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_Type",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "WarehouseTransfers");

            migrationBuilder.DropColumn(
                name: "TransferDate",
                table: "WarehouseTransfers");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleWarehouseId",
                table: "WarehouseTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "StockRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransfers_StockRequests_StockRequestId",
                table: "WarehouseTransfers",
                column: "StockRequestId",
                principalTable: "StockRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

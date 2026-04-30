using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StockMovementColumnsAndTransferDocumentNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FromWarehouseId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToWarehouseId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "StockMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SourceId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDocumentNo",
                table: "StockMovements",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MovementDate",
                table: "StockMovements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE StockMovements SET MovementDate = CreatedAtUtc WHERE MovementDate IS NULL");

            migrationBuilder.AlterColumn<DateTime>(
                name: "MovementDate",
                table: "StockMovements",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "StockMovements",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_FromWarehouseId",
                table: "StockMovements",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ToWarehouseId",
                table: "StockMovements",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SourceType",
                table: "StockMovements",
                column: "SourceType");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Warehouses_FromWarehouseId",
                table: "StockMovements",
                column: "FromWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Warehouses_ToWarehouseId",
                table: "StockMovements",
                column: "ToWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNo",
                table: "WarehouseTransfers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE wt
                SET DocumentNo = CONCAT(N'WT-', RIGHT(CONCAT(N'000000', CAST(wt.Id AS nvarchar(10))), 6))
                FROM WarehouseTransfers AS wt
                WHERE wt.DocumentNo IS NULL
                """);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentNo",
                table: "WarehouseTransfers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_CompanyId_DocumentNo",
                table: "WarehouseTransfers",
                columns: new[] { "CompanyId", "DocumentNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseTransfers_CompanyId_DocumentNo",
                table: "WarehouseTransfers");

            migrationBuilder.DropColumn(
                name: "DocumentNo",
                table: "WarehouseTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_ToWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_FromWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SourceType",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ToWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_FromWarehouseId",
                table: "StockMovements");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "StockMovements",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.DropColumn(
                name: "MovementDate",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "SourceDocumentNo",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ToWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "FromWarehouseId",
                table: "StockMovements");
        }
    }
}

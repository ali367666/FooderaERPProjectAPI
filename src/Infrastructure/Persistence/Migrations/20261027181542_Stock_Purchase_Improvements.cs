using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stock_Purchase_Improvements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "StockPurchases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "StockPurchases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "StockPurchases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "StockPurchases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmountAzn",
                table: "StockPurchases",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SupplierInvoiceNo",
                table: "StockPurchases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageCostAzn",
                table: "StockItems",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastPurchasePriceAzn",
                table: "StockItems",
                type: "decimal(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "StockPurchases");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "StockPurchases");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "StockPurchases");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "StockPurchases");

            migrationBuilder.DropColumn(
                name: "PaidAmountAzn",
                table: "StockPurchases");

            migrationBuilder.DropColumn(
                name: "SupplierInvoiceNo",
                table: "StockPurchases");

            migrationBuilder.DropColumn(
                name: "AverageCostAzn",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "LastPurchasePriceAzn",
                table: "StockItems");
        }
    }
}

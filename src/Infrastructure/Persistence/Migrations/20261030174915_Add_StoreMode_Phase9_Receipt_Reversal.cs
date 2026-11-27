using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StoreMode_Phase9_Receipt_Reversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReverseReason",
                table: "StoreSupplierPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAtUtc",
                table: "StoreSupplierPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversedByUserId",
                table: "StoreSupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowReceiptlessSale",
                table: "StoreSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFooter",
                table: "StoreSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptHeader",
                table: "StoreSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptWidthMm",
                table: "StoreSettings",
                type: "int",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.AddColumn<bool>(
                name: "IsReceiptless",
                table: "StoreSales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReverseReason",
                table: "StoreCustomerPayments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAtUtc",
                table: "StoreCustomerPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversedByUserId",
                table: "StoreCustomerPayments",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReverseReason",
                table: "StoreSupplierPayments");

            migrationBuilder.DropColumn(
                name: "ReversedAtUtc",
                table: "StoreSupplierPayments");

            migrationBuilder.DropColumn(
                name: "ReversedByUserId",
                table: "StoreSupplierPayments");

            migrationBuilder.DropColumn(
                name: "AllowReceiptlessSale",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "ReceiptFooter",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "ReceiptHeader",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "ReceiptWidthMm",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "IsReceiptless",
                table: "StoreSales");

            migrationBuilder.DropColumn(
                name: "ReverseReason",
                table: "StoreCustomerPayments");

            migrationBuilder.DropColumn(
                name: "ReversedAtUtc",
                table: "StoreCustomerPayments");

            migrationBuilder.DropColumn(
                name: "ReversedByUserId",
                table: "StoreCustomerPayments");
        }
    }
}

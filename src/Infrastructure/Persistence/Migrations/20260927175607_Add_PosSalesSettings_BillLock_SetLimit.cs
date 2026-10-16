using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_PosSalesSettings_BillLock_SetLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BillPrintedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBillLocked",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Limit",
                table: "MenuItemSetComponents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockOrderAfterBill",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentCardEnabled",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentCashEnabled",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentCreditEnabled",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PosMarsEnabled",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintKitchenSeparateTickets",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireProductCode",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WaiterCanCancel",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WaiterConfirmWithPin",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillPrintedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsBillLocked",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Limit",
                table: "MenuItemSetComponents");

            migrationBuilder.DropColumn(
                name: "LockOrderAfterBill",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PaymentCardEnabled",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PaymentCashEnabled",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PaymentCreditEnabled",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PosMarsEnabled",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PrintKitchenSeparateTickets",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "RequireProductCode",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "WaiterCanCancel",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "WaiterConfirmWithPin",
                table: "CompanySettings");
        }
    }
}

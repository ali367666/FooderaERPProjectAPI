using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_PrintPanelSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrintAutoOnPayment",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintGroupQuantities",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrintShowPreview",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowOrderNumber",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowPaymentMethod",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowTableName",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowTime",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowWaiterName",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintAutoOnPayment",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PrintGroupQuantities",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PrintShowPreview",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptShowOrderNumber",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptShowPaymentMethod",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptShowTableName",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptShowTime",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptShowWaiterName",
                table: "CompanySettings");
        }
    }
}

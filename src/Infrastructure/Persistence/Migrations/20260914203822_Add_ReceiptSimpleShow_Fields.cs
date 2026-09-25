using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_ReceiptSimpleShow_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReceiptSimpleShowFooter",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptSimpleShowOrderNumber",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptSimpleShowPaymentMethod",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptSimpleShowTime",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptSimpleShowVat",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptSimpleShowWaiterName",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptSimpleShowFooter",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptSimpleShowOrderNumber",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptSimpleShowPaymentMethod",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptSimpleShowTime",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptSimpleShowVat",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptSimpleShowWaiterName",
                table: "CompanySettings");
        }
    }
}

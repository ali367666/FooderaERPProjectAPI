using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_PrintPanel_TransferDoc_ChiefPrint_BusinessName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsChiefPrinter",
                table: "Printers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintChiefCopy",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintKitchenOnHold",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintKitchenShowBusinessName",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrintTransferDocAuto",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintTransferDocDouble",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowBusinessName",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsChiefPrinter",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "PrintChiefCopy",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PrintKitchenOnHold",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PrintKitchenShowBusinessName",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PrintTransferDocAuto",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PrintTransferDocDouble",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptShowBusinessName",
                table: "CompanySettings");
        }
    }
}

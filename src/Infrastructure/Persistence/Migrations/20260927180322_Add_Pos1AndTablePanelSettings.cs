using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Pos1AndTablePanelSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PosShowChangePanel",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PosShowClock",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PosShowCustomerSelect",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PosShowHoldButton",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PosShowWeighWindow",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PriceFromWarehouseSale",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TableBusyWarning",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TablePricesFromStation",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TableReservationWarning",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TableShowAmount",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TableShowNote",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TableShowTime",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TableShowWaiter",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosShowChangePanel",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PosShowClock",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PosShowCustomerSelect",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PosShowHoldButton",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PosShowWeighWindow",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PriceFromWarehouseSale",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TableBusyWarning",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TablePricesFromStation",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TableReservationWarning",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TableShowAmount",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TableShowNote",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TableShowTime",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TableShowWaiter",
                table: "CompanySettings");
        }
    }
}

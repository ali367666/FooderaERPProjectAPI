using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_ThemeReceiptDesignSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptFooterText",
                table: "CompanySettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptHeaderText",
                table: "CompanySettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptPaperWidth",
                table: "CompanySettings",
                type: "int",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptSortMode",
                table: "CompanySettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "order");

            migrationBuilder.AddColumn<string>(
                name: "ThemePrimaryColor",
                table: "CompanySettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeRadius",
                table: "CompanySettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptFooterText",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptHeaderText",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptPaperWidth",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptSortMode",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ThemePrimaryColor",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ThemeRadius",
                table: "CompanySettings");
        }
    }
}

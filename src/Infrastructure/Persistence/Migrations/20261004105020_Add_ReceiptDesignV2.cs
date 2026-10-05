using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_ReceiptDesignV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptGiftNote",
                table: "CompanySettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptGiftNoteFontSize",
                table: "CompanySettings",
                type: "int",
                nullable: false,
                defaultValue: 18);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptLogoWidth",
                table: "CompanySettings",
                type: "int",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowLogo",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptSocialPosition",
                table: "CompanySettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "top");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptGiftNote",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptGiftNoteFontSize",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptLogoWidth",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptShowLogo",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptSocialPosition",
                table: "CompanySettings");
        }
    }
}

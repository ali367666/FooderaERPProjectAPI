using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_SetPrinterId_And_PurchasePrice5Decimals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PurchasePrice",
                table: "MenuItems",
                type: "decimal(18,5)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SetPrinterId",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_SetPrinterId",
                table: "MenuItems",
                column: "SetPrinterId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Printers_SetPrinterId",
                table: "MenuItems",
                column: "SetPrinterId",
                principalTable: "Printers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Printers_SetPrinterId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_SetPrinterId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SetPrinterId",
                table: "MenuItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchasePrice",
                table: "MenuItems",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)",
                oldNullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantTableLayoutColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "RestaurantTables",
                type: "int",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.AddColumn<int>(
                name: "PosX",
                table: "RestaurantTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PosY",
                table: "RestaurantTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rotation",
                table: "RestaurantTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Shape",
                table: "RestaurantTables",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "square");

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "RestaurantTables",
                type: "int",
                nullable: false,
                defaultValue: 80);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "PosX",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "PosY",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "Rotation",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "Shape",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "RestaurantTables");
        }
    }
}

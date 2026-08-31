using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_ExtraCompanyModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ModuleDataSecimi",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleFitnes",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleOtel",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModulePaket",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleQiymetSor",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModuleDataSecimi",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ModuleFitnes",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ModuleOtel",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ModulePaket",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ModuleQiymetSor",
                table: "CompanySettings");
        }
    }
}

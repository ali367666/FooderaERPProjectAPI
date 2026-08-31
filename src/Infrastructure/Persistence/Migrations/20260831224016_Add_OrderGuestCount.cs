using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_OrderGuestCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GuestCount",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AskGuestCountOnOpen",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestCount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AskGuestCountOnOpen",
                table: "CompanySettings");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Position_RoleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Positions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_CompanyId_RoleId",
                table: "Positions",
                columns: new[] { "CompanyId", "RoleId" },
                unique: true,
                filter: "[RoleId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Positions_CompanyId_RoleId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Positions");
        }
    }
}

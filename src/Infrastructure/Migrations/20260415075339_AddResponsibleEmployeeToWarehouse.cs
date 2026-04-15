using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsibleEmployeeToWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsibleEmployeeId",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_ResponsibleEmployeeId",
                table: "Warehouses",
                column: "ResponsibleEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Employees_ResponsibleEmployeeId",
                table: "Warehouses",
                column: "ResponsibleEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Employees_ResponsibleEmployeeId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_ResponsibleEmployeeId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ResponsibleEmployeeId",
                table: "Warehouses");
        }
    }
}

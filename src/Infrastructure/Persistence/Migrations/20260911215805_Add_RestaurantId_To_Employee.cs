using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_RestaurantId_To_Employee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill existing employees to their own company's (first) branch before the FK is
            // added below — a fresh 0 would violate it.
            migrationBuilder.Sql(@"
                UPDATE e
                SET e.RestaurantId = (
                    SELECT TOP 1 r.Id
                    FROM Restaurants r
                    WHERE r.CompanyId = e.CompanyId
                    ORDER BY r.Id
                )
                FROM Employees e;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RestaurantId",
                table: "Employees",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Restaurants_RestaurantId",
                table: "Employees",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Restaurants_RestaurantId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_RestaurantId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "Employees");
        }
    }
}

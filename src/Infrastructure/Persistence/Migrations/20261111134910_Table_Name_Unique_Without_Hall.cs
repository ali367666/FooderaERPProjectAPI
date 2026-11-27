using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Table_Name_Unique_Without_Hall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_CompanyId_RestaurantId_Name_NoSection",
                table: "RestaurantTables",
                columns: new[] { "CompanyId", "RestaurantId", "Name" },
                unique: true,
                filter: "[SectionId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_CompanyId_RestaurantId_Name_NoSection",
                table: "RestaurantTables");
        }
    }
}

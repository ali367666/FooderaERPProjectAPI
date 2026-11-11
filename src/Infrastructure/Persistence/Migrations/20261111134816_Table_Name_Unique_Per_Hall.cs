using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Table_Name_Unique_Per_Hall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_CompanyId_Name",
                table: "RestaurantTables");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_CompanyId_RestaurantId_SectionId_Name",
                table: "RestaurantTables",
                columns: new[] { "CompanyId", "RestaurantId", "SectionId", "Name" },
                unique: true,
                filter: "[SectionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_CompanyId_RestaurantId_SectionId_Name",
                table: "RestaurantTables");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_CompanyId_Name",
                table: "RestaurantTables",
                columns: new[] { "CompanyId", "Name" },
                unique: true);
        }
    }
}

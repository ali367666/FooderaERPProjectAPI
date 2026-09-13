using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Kompleks_RestaurantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ModuleKompleks",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RestaurantSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    ModuleAnbar = table.Column<bool>(type: "bit", nullable: false),
                    ModuleRezervasyon = table.Column<bool>(type: "bit", nullable: false),
                    ModuleMasaBolge = table.Column<bool>(type: "bit", nullable: false),
                    ModulePaket = table.Column<bool>(type: "bit", nullable: false),
                    ModuleOtel = table.Column<bool>(type: "bit", nullable: false),
                    ModuleFitnes = table.Column<bool>(type: "bit", nullable: false),
                    ModuleDataSecimi = table.Column<bool>(type: "bit", nullable: false),
                    ModuleQiymetSor = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantSettings_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantSettings_RestaurantId",
                table: "RestaurantSettings",
                column: "RestaurantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "ModuleKompleks",
                table: "CompanySettings");
        }
    }
}

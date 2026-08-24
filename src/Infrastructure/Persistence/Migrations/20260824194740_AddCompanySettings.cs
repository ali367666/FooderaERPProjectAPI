using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpeningTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    ModuleFilial = table.Column<bool>(type: "bit", nullable: false),
                    ModulePaket = table.Column<bool>(type: "bit", nullable: false),
                    ModuleOtel = table.Column<bool>(type: "bit", nullable: false),
                    ModuleAnbar = table.Column<bool>(type: "bit", nullable: false),
                    ModuleFitnes = table.Column<bool>(type: "bit", nullable: false),
                    ModuleRezervasyon = table.Column<bool>(type: "bit", nullable: false),
                    ModuleDataSecimi = table.Column<bool>(type: "bit", nullable: false),
                    ModuleMasaBolge = table.Column<bool>(type: "bit", nullable: false),
                    ModuleQiymetSor = table.Column<bool>(type: "bit", nullable: false),
                    IntegrationWolt = table.Column<bool>(type: "bit", nullable: false),
                    IntegrationBolt = table.Column<bool>(type: "bit", nullable: false),
                    Integration189Delivery = table.Column<bool>(type: "bit", nullable: false),
                    AlertMilliseconds = table.Column<int>(type: "int", nullable: true),
                    AlertRingCount = table.Column<int>(type: "int", nullable: true),
                    AlertRingIntervalSeconds = table.Column<int>(type: "int", nullable: true),
                    LoginLogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReportLogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WallpaperUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LoginLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransparencyLevel = table.Column<int>(type: "int", nullable: true),
                    ProductColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FloorLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Slogan = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SocialLinks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContactPhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReceiptFontSize = table.Column<int>(type: "int", nullable: true),
                    CategoryFontSize = table.Column<int>(type: "int", nullable: true),
                    AllowReceiptEditAfterPrint = table.Column<bool>(type: "bit", nullable: false),
                    WaiterCanPrintCustomerReceipt = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySettings_CompanyId",
                table: "CompanySettings",
                column: "CompanyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySettings");
        }
    }
}

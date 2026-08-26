using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrinterStationTypeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StationType",
                table: "Printers",
                newName: "StationTypeId");

            migrationBuilder.CreateTable(
                name: "PrinterStationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterStationTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrinterStationTypes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Printers_StationTypeId",
                table: "Printers",
                column: "StationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterStationTypes_CompanyId_Name",
                table: "PrinterStationTypes",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Printers_PrinterStationTypes_StationTypeId",
                table: "Printers",
                column: "StationTypeId",
                principalTable: "PrinterStationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Printers_PrinterStationTypes_StationTypeId",
                table: "Printers");

            migrationBuilder.DropTable(
                name: "PrinterStationTypes");

            migrationBuilder.DropIndex(
                name: "IX_Printers_StationTypeId",
                table: "Printers");

            migrationBuilder.RenameColumn(
                name: "StationTypeId",
                table: "Printers",
                newName: "StationType");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MenuItemTypesEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ItemType",
                table: "MenuItems",
                newName: "ItemTypeId");

            migrationBuilder.CreateTable(
                name: "MenuItemTypes",
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
                    table.PrimaryKey("PK_MenuItemTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemTypes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ItemTypeId",
                table: "MenuItems",
                column: "ItemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemTypes_CompanyId_Name",
                table: "MenuItemTypes",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_MenuItemTypes_ItemTypeId",
                table: "MenuItems",
                column: "ItemTypeId",
                principalTable: "MenuItemTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuItemTypes_ItemTypeId",
                table: "MenuItems");

            migrationBuilder.DropTable(
                name: "MenuItemTypes");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_ItemTypeId",
                table: "MenuItems");

            migrationBuilder.RenameColumn(
                name: "ItemTypeId",
                table: "MenuItems",
                newName: "ItemType");
        }
    }
}

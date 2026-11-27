using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StoreMode_Phase7_Campaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CampaignFromUtc",
                table: "StoreProducts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CampaignPrice",
                table: "StoreProducts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CampaignToUtc",
                table: "StoreProducts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampaignFromUtc",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "CampaignPrice",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "CampaignToUtc",
                table: "StoreProducts");
        }
    }
}

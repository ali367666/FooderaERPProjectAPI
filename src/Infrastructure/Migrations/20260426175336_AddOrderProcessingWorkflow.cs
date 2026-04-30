using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderProcessingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedByUserId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProcessedByUserId",
                table: "Orders",
                column: "ProcessedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_ProcessedByUserId",
                table: "Orders",
                column: "ProcessedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_ProcessedByUserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ProcessedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "Orders");
        }
    }
}

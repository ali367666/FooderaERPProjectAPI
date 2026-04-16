using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestedByUserToStockRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestedByUserId",
                table: "StockRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockRequests_RequestedByUserId",
                table: "StockRequests",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockRequests_AspNetUsers_RequestedByUserId",
                table: "StockRequests",
                column: "RequestedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockRequests_AspNetUsers_RequestedByUserId",
                table: "StockRequests");

            migrationBuilder.DropIndex(
                name: "IX_StockRequests_RequestedByUserId",
                table: "StockRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "StockRequests");
        }
    }
}

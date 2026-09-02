using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_OrderCounterparty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CounterpartyId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CounterpartyId",
                table: "Orders",
                column: "CounterpartyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Counterparties_CounterpartyId",
                table: "Orders",
                column: "CounterpartyId",
                principalTable: "Counterparties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Counterparties_CounterpartyId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CounterpartyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CounterpartyId",
                table: "Orders");
        }
    }
}

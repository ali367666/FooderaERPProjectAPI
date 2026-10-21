using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_PaymentBanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankCounterpartyId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankCounterpartyId",
                table: "OrderPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBank",
                table: "Counterparties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BankCounterpartyId",
                table: "Orders",
                column: "BankCounterpartyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_BankCounterpartyId",
                table: "OrderPayments",
                column: "BankCounterpartyId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPayments_Counterparties_BankCounterpartyId",
                table: "OrderPayments",
                column: "BankCounterpartyId",
                principalTable: "Counterparties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Counterparties_BankCounterpartyId",
                table: "Orders",
                column: "BankCounterpartyId",
                principalTable: "Counterparties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderPayments_Counterparties_BankCounterpartyId",
                table: "OrderPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Counterparties_BankCounterpartyId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BankCounterpartyId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderPayments_BankCounterpartyId",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "BankCounterpartyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BankCounterpartyId",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "IsBank",
                table: "Counterparties");
        }
    }
}

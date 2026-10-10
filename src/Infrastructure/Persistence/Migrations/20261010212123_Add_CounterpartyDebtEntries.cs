using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_CounterpartyDebtEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CounterpartyDebtEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CounterpartyId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterpartyDebtEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CounterpartyDebtEntries_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CounterpartyDebtEntries_Counterparties_CounterpartyId",
                        column: x => x.CounterpartyId,
                        principalTable: "Counterparties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CounterpartyDebtEntries_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CounterpartyDebtEntries_CompanyId",
                table: "CounterpartyDebtEntries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CounterpartyDebtEntries_CounterpartyId_CreatedAtUtc",
                table: "CounterpartyDebtEntries",
                columns: new[] { "CounterpartyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CounterpartyDebtEntries_OrderId",
                table: "CounterpartyDebtEntries",
                column: "OrderId");

            // Counterparties that already owe money start their history with that balance, so the
            // list is not empty. Type 2 = Adjusted.
            migrationBuilder.Sql(@"
                INSERT INTO CounterpartyDebtEntries
                    (CompanyId, CounterpartyId, [Type], Amount, BalanceAfter, Note, CreatedAtUtc)
                SELECT CompanyId, Id, 2, CurrentDebtAmount, CurrentDebtAmount,
                       N'Əvvəlki borc (tarixçə başlamazdan əvvəl)', SYSUTCDATETIME()
                FROM Counterparties
                WHERE CurrentDebtAmount <> 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CounterpartyDebtEntries");
        }
    }
}

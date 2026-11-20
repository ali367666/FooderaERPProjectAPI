using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_StoreExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreExpenseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreExpenseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreExpenseItems_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreExpenses_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreExpenseLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreExpenseId = table.Column<int>(type: "int", nullable: false),
                    StoreCashRegisterId = table.Column<int>(type: "int", nullable: false),
                    StoreCashShiftId = table.Column<int>(type: "int", nullable: false),
                    StoreExpenseItemId = table.Column<int>(type: "int", nullable: false),
                    Payee = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StoreCashMovementId = table.Column<int>(type: "int", nullable: true),
                    ReversalMovementId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreExpenseLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreExpenseLines_StoreCashRegisters_StoreCashRegisterId",
                        column: x => x.StoreCashRegisterId,
                        principalTable: "StoreCashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreExpenseLines_StoreExpenseItems_StoreExpenseItemId",
                        column: x => x.StoreExpenseItemId,
                        principalTable: "StoreExpenseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreExpenseLines_StoreExpenses_StoreExpenseId",
                        column: x => x.StoreExpenseId,
                        principalTable: "StoreExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenseItems_CompanyId_Name",
                table: "StoreExpenseItems",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenseLines_StoreCashRegisterId",
                table: "StoreExpenseLines",
                column: "StoreCashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenseLines_StoreExpenseId",
                table: "StoreExpenseLines",
                column: "StoreExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenseLines_StoreExpenseItemId",
                table: "StoreExpenseLines",
                column: "StoreExpenseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenses_CompanyId_DocumentDate",
                table: "StoreExpenses",
                columns: new[] { "CompanyId", "DocumentDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreExpenseLines");

            migrationBuilder.DropTable(
                name: "StoreExpenseItems");

            migrationBuilder.DropTable(
                name: "StoreExpenses");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserDb.Migrations
{
    public partial class _23071201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoppingOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confirm1 = table.Column<bool>(type: "bit", nullable: false),
                    Confirm2 = table.Column<bool>(type: "bit", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    BinaryArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreateUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameShoppingOrderDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Count = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BinaryArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    GameShoppingOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameShoppingOrderDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameShoppingOrderDetail_ShoppingOrder_GameShoppingOrderId",
                        column: x => x.GameShoppingOrderId,
                        principalTable: "ShoppingOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameShoppingOrderDetail_GameShoppingOrderId",
                table: "GameShoppingOrderDetail",
                column: "GameShoppingOrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameShoppingOrderDetail");

            migrationBuilder.DropTable(
                name: "ShoppingOrder");
        }
    }
}

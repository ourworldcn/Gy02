using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserDb.Migrations
{
    public partial class _23112002 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GameRedeemCode",
                table: "GameRedeemCode");

            migrationBuilder.RenameTable(
                name: "GameRedeemCode",
                newName: "GameRedeemCodes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameRedeemCodes",
                table: "GameRedeemCodes",
                column: "Code");

            migrationBuilder.CreateTable(
                name: "GameRedeemCodeCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名称"),
                    CodeType = table.Column<int>(type: "int", nullable: false, comment: "生成的码的类型，1=通用码，2=一次性码。"),
                    ShoppingTId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "兑换码使用的商品TId")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRedeemCodeCatalogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameRedeemCodeCatalogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameRedeemCodes",
                table: "GameRedeemCodes");

            migrationBuilder.RenameTable(
                name: "GameRedeemCodes",
                newName: "GameRedeemCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameRedeemCode",
                table: "GameRedeemCode",
                column: "Code");
        }
    }
}

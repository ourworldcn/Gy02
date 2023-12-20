using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserDb.Migrations
{
    public partial class _23112001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameRedeemCode",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, comment: "兑换码，也是Id。"),
                    CatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRedeemCode", x => x.Code);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameRedeemCode");
        }
    }
}

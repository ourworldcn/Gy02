using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _22112101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemTemplates");

            migrationBuilder.CreateTable(
                name: "ThingTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GId = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GenusString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThingTemplates", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThingTemplates");

            migrationBuilder.CreateTable(
                name: "ItemTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    备注 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GId = table.Column<int>(type: "int", nullable: true),
                    GenusIdString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Script = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTemplates", x => x.Id);
                });
        }
    }
}

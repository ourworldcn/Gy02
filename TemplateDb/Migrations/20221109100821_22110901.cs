using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _22110901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    备注 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GId = table.Column<int>(type: "int", nullable: true),
                    GenusIdString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PropertiesString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Script = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTemplates", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemTemplates");
        }
    }
}

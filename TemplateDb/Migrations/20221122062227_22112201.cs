using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _22112201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThingTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GenusString = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "属信息，逗号分隔的字符串。"),
                    ExtraLong = table.Column<long>(type: "bigint", nullable: true, comment: "扩展的长整型信息。"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "注释信息，服务器不使用该字段。")
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
        }
    }
}

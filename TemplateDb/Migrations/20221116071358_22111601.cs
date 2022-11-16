using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _22111601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PropertiesString",
                table: "ItemTemplates",
                newName: "JsonObjectString");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JsonObjectString",
                table: "ItemTemplates",
                newName: "PropertiesString");
        }
    }
}

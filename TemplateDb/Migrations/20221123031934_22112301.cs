using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _22112301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenusString",
                table: "ThingTemplates");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GenusString",
                table: "ThingTemplates",
                type: "nvarchar(max)",
                nullable: true,
                comment: "属信息，逗号分隔的字符串。")
                .Annotation("Relational:ColumnOrder", 20);
        }
    }
}

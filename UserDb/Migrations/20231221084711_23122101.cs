using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserDb.Migrations
{
    public partial class _23122101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrphanedThings");

            migrationBuilder.AlterColumn<string>(
                name: "ExtraString",
                table: "VirtualThings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ExtraDecimal",
                table: "VirtualThings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "记录一些额外的数值信息，用于排序搜索使用的字段",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExtraDateTime",
                table: "VirtualThings",
                type: "datetime2",
                nullable: true,
                comment: "记录扩展的日期时间属性");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraDateTime",
                table: "VirtualThings");

            migrationBuilder.AlterColumn<string>(
                name: "ExtraString",
                table: "VirtualThings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExtraDecimal",
                table: "VirtualThings",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "记录一些额外的数值信息，用于排序搜索使用的字段");

            migrationBuilder.CreateTable(
                name: "OrphanedThings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BinaryArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExtraGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraString = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrphanedThings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrphanedThings_ExtraGuid_ExtraDecimal_ExtraString",
                table: "OrphanedThings",
                columns: new[] { "ExtraGuid", "ExtraDecimal", "ExtraString" });

            migrationBuilder.CreateIndex(
                name: "IX_OrphanedThings_ExtraGuid_ExtraString_ExtraDecimal",
                table: "OrphanedThings",
                columns: new[] { "ExtraGuid", "ExtraString", "ExtraDecimal" });
        }
    }
}

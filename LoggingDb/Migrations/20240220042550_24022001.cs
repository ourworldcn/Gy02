using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoggingDb.Migrations
{
    public partial class _24022001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActionRecords_ActionId_DateTimeUtc",
                table: "ActionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActionRecords_DateTimeUtc_ActionId",
                table: "ActionRecords");

            migrationBuilder.DropColumn(
                name: "DateTimeUtc",
                table: "ActionRecords");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ActionRecords");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "ActionRecords");

            migrationBuilder.AlterColumn<string>(
                name: "ActionId",
                table: "ActionRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "行为Id。如Logined , ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==。",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraDecimal",
                table: "ActionRecords",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                comment: "额外数字，具体意义取决于该条记录的类型。");

            migrationBuilder.AddColumn<Guid>(
                name: "ExtraGuid",
                table: "ActionRecords",
                type: "uniqueidentifier",
                nullable: true,
                comment: "额外Guid。");

            migrationBuilder.AddColumn<string>(
                name: "ExtraString",
                table: "ActionRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "额外的字符串，通常行为Id，最长64字符。");

            migrationBuilder.AddColumn<DateTime>(
                name: "WorldDateTime",
                table: "ActionRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "这个行为发生的世界时间。");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ActionId_WorldDateTime",
                table: "ActionRecords",
                columns: new[] { "ActionId", "WorldDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_WorldDateTime_ActionId",
                table: "ActionRecords",
                columns: new[] { "WorldDateTime", "ActionId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActionRecords_ActionId_WorldDateTime",
                table: "ActionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActionRecords_WorldDateTime_ActionId",
                table: "ActionRecords");

            migrationBuilder.DropColumn(
                name: "ExtraDecimal",
                table: "ActionRecords");

            migrationBuilder.DropColumn(
                name: "ExtraGuid",
                table: "ActionRecords");

            migrationBuilder.DropColumn(
                name: "ExtraString",
                table: "ActionRecords");

            migrationBuilder.DropColumn(
                name: "WorldDateTime",
                table: "ActionRecords");

            migrationBuilder.AlterColumn<string>(
                name: "ActionId",
                table: "ActionRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "行为Id。如Logined , ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==。");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTimeUtc",
                table: "ActionRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "ActionRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "ActionRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ActionId_DateTimeUtc",
                table: "ActionRecords",
                columns: new[] { "ActionId", "DateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_DateTimeUtc_ActionId",
                table: "ActionRecords",
                columns: new[] { "DateTimeUtc", "ActionId" });
        }
    }
}

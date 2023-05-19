using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoggingDb.Migrations
{
    public partial class _23051901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ActionId_DateTimeUtc",
                table: "ActionRecords",
                columns: new[] { "ActionId", "DateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_DateTimeUtc_ActionId",
                table: "ActionRecords",
                columns: new[] { "DateTimeUtc", "ActionId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionRecords");
        }
    }
}

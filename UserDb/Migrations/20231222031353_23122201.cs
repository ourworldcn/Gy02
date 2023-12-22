using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserDb.Migrations
{
    public partial class _23122201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletionDateTime",
                table: "ShoppingOrder",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionDateTime",
                table: "ShoppingOrder");
        }
    }
}

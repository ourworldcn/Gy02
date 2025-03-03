using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserDb.Migrations
{
    public partial class _24110101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingOrder_CustomerId_CreateUtc",
                table: "ShoppingOrder");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "ShoppingOrder",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingOrder_Channel_CustomerId_CreateUtc",
                table: "ShoppingOrder",
                columns: new[] { "Channel", "CustomerId", "CreateUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingOrder_Channel_CustomerId_CreateUtc",
                table: "ShoppingOrder");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "ShoppingOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingOrder_CustomerId_CreateUtc",
                table: "ShoppingOrder",
                columns: new[] { "CustomerId", "CreateUtc" });
        }
    }
}

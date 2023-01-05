using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserDb.Migrations
{
    public partial class _23010501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameUsers");

            migrationBuilder.CreateTable(
                name: "OrphanedThings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BinaryArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExtraGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraString = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrphanedThings");

            migrationBuilder.CreateTable(
                name: "GameUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BinaryArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExtraGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraString = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameUsers_ExtraGuid_ExtraDecimal_ExtraString",
                table: "GameUsers",
                columns: new[] { "ExtraGuid", "ExtraDecimal", "ExtraString" });

            migrationBuilder.CreateIndex(
                name: "IX_GameUsers_ExtraGuid_ExtraString_ExtraDecimal",
                table: "GameUsers",
                columns: new[] { "ExtraGuid", "ExtraString", "ExtraDecimal" });
        }
    }
}

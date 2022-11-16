using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserDb.Migrations
{
    public partial class _22111601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraString = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BinaryArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VirtualThings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    BinaryArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtraGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraString = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualThings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualThings_VirtualThings_ParentId",
                        column: x => x.ParentId,
                        principalTable: "VirtualThings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameUsers_ExtraGuid_ExtraDecimal_ExtraString",
                table: "GameUsers",
                columns: new[] { "ExtraGuid", "ExtraDecimal", "ExtraString" });

            migrationBuilder.CreateIndex(
                name: "IX_GameUsers_ExtraGuid_ExtraString_ExtraDecimal",
                table: "GameUsers",
                columns: new[] { "ExtraGuid", "ExtraString", "ExtraDecimal" });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualThings_ExtraGuid_ExtraDecimal_ExtraString",
                table: "VirtualThings",
                columns: new[] { "ExtraGuid", "ExtraDecimal", "ExtraString" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualThings_ExtraGuid_ExtraString_ExtraDecimal",
                table: "VirtualThings",
                columns: new[] { "ExtraGuid", "ExtraString", "ExtraDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualThings_ParentId",
                table: "VirtualThings",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameUsers");

            migrationBuilder.DropTable(
                name: "VirtualThings");
        }
    }
}

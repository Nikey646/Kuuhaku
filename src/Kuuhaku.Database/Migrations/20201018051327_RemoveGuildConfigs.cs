using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kuuhaku.Database.Migrations
{
    public partial class RemoveGuildConfigs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildConfigs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    CommandSeperator = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Prefix = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.Id);
                });
        }
    }
}

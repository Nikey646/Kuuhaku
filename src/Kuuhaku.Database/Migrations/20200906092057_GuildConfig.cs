using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kuuhaku.Database.Migrations
{
    public partial class GuildConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    Prefix = table.Column<string>(nullable: true),
                    CommandSeperator = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildConfigs");
        }
    }
}

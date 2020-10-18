using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kuuhaku.Database.Migrations
{
    public partial class RemovReminder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reminders");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Contents = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RemindAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                });
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kuuhaku.Database.Migrations
{
    public partial class ReminderTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    RemindAt = table.Column<DateTime>(nullable: false),
                    Contents = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reminders");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kuuhaku.Database.Migrations
{
    public partial class UserRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRoleLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: true),
                    RoleId = table.Column<ulong>(nullable: false),
                    EmojiId = table.Column<ulong>(nullable: true),
                    EmojiName = table.Column<string>(nullable: true),
                    ShortDescription = table.Column<string>(maxLength: 200, nullable: true),
                    UserRoleLocationId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_UserRoleLocations_UserRoleLocationId",
                        column: x => x.UserRoleLocationId,
                        principalTable: "UserRoleLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserRoleLocationId",
                table: "UserRoles",
                column: "UserRoleLocationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserRoleLocations");
        }
    }
}

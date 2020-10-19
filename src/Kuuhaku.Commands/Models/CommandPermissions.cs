using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kuuhaku.Commands.Models
{
    public enum CommandPermissions
    {
        Developer = 25,
        BotOwner = 20,
        ServerOwner = 15,
        Admin = 10,
        Moderator = 5,
        Everyone = 0,
    }
}

using System;

namespace Kuuhaku.Commands.Models
{
    public class RepeatCommandDto
    {
        public String Command { get; set; }
        public UInt64 GuildId { get; set; }
        public UInt64 UserId { get; set; }

        public RepeatCommandDto(String command, UInt64 guildId, UInt64 userId)
        {
            this.Command = command;
            this.GuildId = guildId;
            this.UserId = userId;
        }
    }
}

using System;

namespace Kuuhaku.Commands.Models
{
    public class GuildConfig
    {
        public UInt64 GuildId { get; set; }
        public String Prefix { get; set; }
        public String CommandSeperator { get; set; }

        public GuildConfig()
        {
            this.Prefix = "!";
            this.CommandSeperator = "//";
        }

        public GuildConfig(UInt64 guildGuildId) : this()
        {
            this.GuildId = guildGuildId;
        }
    }
}

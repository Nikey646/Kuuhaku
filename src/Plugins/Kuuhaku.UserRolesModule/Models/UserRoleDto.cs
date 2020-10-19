using System;
using System.Reflection.Metadata.Ecma335;

namespace Kuuhaku.UserRolesModule.Models
{
    public class UserRoleDto
    {
        public UInt64 GuildId { get; set; }
        public UInt64 ChannelId { get; set; }

        public UInt64? MessageId { get; set; }
        public UInt64 RoleId { get; set; }

        public UInt64? EmojiId { get; set; }
        public String EmojiName { get; set; }

        public String ShortDescription { get; set; }

        public UserRoleDto(UInt64 guildId, UInt64 channelId, UInt64 roleId, UInt64? emojiId, String emojiName, String shortDescription)
        {
            this.GuildId = guildId;
            this.ChannelId = channelId;
            this.RoleId = roleId;
            this.EmojiId = emojiId;
            this.EmojiName = emojiName;
            this.ShortDescription = shortDescription;
        }
    }
}

using System;

namespace Kuuhaku.ReminderModule.Models
{
    public class ReminderDto
    {
        // Apart of the key
        public UInt64 GuildId { get; set; }
        public UInt64 ChannelId { get; set; }
        public UInt64 MessageId { get; set; }

        public UInt64 UserId { get; set; }

        public DateTime RemindAt { get; set; }
        public String Contents { get; set; }

        public Boolean IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public ReminderDto()
        {
            this.CreatedAt = DateTime.UtcNow;
            this.IsActive = true;
        }

        public ReminderDto(UInt64 guildId, UInt64 channelId, UInt64 messageId, UInt64 userId, DateTime when, String what) : this()
        {
            this.GuildId = guildId;
            this.ChannelId = channelId;
            this.MessageId = messageId;
            this.UserId = userId;
            this.RemindAt = when;
            this.Contents = what;
        }
    }
}

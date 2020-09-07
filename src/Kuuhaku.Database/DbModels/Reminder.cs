using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuuhaku.Database.DbModels
{
    public class Reminder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public UInt64? GuildId { get; set; }
        public UInt64 ChannelId { get; set; }
        public UInt64 UserId { get; set; }

        public DateTime RemindAt { get; set; }
        public String Contents { get; set; }

        public Boolean IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // EF Core ctor
        public Reminder()
        {
            this.CreatedAt = DateTime.UtcNow;
        }

        public Reminder(UInt64? guildId, UInt64 channelId, UInt64 userId, DateTime when, String what) : this()
        {
            this.GuildId = guildId;
            this.ChannelId = channelId;
            this.UserId = userId;
            this.RemindAt = when;
            this.Contents = what;

            this.IsActive = true;
        }
    }
}

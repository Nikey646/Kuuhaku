using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuuhaku.Database.DbModels
{
    public class UserRoleLocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public UInt64 GuildId { get; set; }
        public UInt64 ChannelId { get; set; }

        // If this is null, there isn't a message.
        public ICollection<UInt64> MessageIds { get; set; } = new List<UInt64>();

        public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    }
}

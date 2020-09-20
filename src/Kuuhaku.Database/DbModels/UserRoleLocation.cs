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

        public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    }
}

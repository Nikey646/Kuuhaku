using System;
using System.Collections.Generic;
using Kuuhaku.Database.DbModels;

namespace Kuuhaku.UserRolesModule.Models
{
    // Rto = Run Time Object
    public class UserRoleRto
    {
        public UInt64? GuildId { get; set; }
        public UInt64 ChannelId { get; set; }

        public ICollection<UInt64> MessageIds { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
    }
}

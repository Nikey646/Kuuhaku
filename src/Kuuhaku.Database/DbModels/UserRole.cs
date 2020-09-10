using System;

namespace Kuuhaku.Database.DbModels
{
    public class UserRole
    {
        public Guid Id { get; set; }

        public UInt64 RoleId { get; set; }

        public UInt64? EmojiId { get; set; }
        public String EmojiName { get; set; }

        public String ShortDescription { get; set; }

        // public UserRoleLocation Location { get; set; }
    }
}

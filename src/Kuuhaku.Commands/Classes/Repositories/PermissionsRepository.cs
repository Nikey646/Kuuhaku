using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Kuuhaku.Commands.Classes.Repositories
{
    public class PermissionsRepository
    {
        private IRedisDatabase _db;

        public PermissionsRepository(IRedisCacheClient redis)
        {
            this._db = redis.Db0;
        }

        public async Task<IEnumerable<UInt64>> GetRolesAsync(IGuild guild, String roleType)
        {
            var roleKey = $"guildConfigs:{guild.Id}:{roleType}";

            var roles = await this._db.Database.SetMembersAsync(roleKey);
            return roles.Select(v => (UInt64) v);
        }

        public Task AddRoleAsync(IGuild guild, String roleType, IRole role)
        {
            var roleKey = $"guildConfigs:{guild.Id}:{roleType}";

            return this._db.Database.SetAddAsync(roleKey, role.Id);
        }

        public Task RemoveRoleAsync(IGuild guild, String roleType, IRole role)
        {
            var roleKey = $"guildConfigs:{guild.Id}:{roleType}";

            return this._db.Database.SetRemoveAsync(roleKey, role.Id);
        }

        public Task<Boolean> ExistsAsync(IGuild guild, String roleType, IRole role)
        {
            var roleKey = $"guildConfigs:{guild.Id}:{roleType}";

            return this._db.Database.SetContainsAsync(roleKey, role.Id);
        }

    }
}

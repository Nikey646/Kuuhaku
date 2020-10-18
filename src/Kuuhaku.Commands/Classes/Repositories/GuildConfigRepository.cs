using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Kuuhaku.Commands.Models;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Kuuhaku.Commands.Classes.Repositories
{
    public class GuildConfigRepository
    {
        private IRedisDatabase _db;

        public GuildConfigRepository(IRedisCacheClient redis)
        {
            this._db = redis.Db0;
        }

        public async Task<GuildConfig> CreateAsync(IGuild guild)
        {
            var gc = new GuildConfig(guild.Id);
            await this.UpdateAsync(guild, gc);
            return gc;
        }

        public async Task UpdateAsync(IGuild guild, GuildConfig config)
        {
            var guildConfigKey = $"guildConfig:{guild.Id}";

            await this._db.AddAsync(guildConfigKey, config);
        }

        public async Task<GuildConfig> GetAsync(IGuild guild)
        {
            var guildConfigKey = $"guildConfig:{guild.Id}";

            var guildConfig = await this._db.GetAsync<GuildConfig>(guildConfigKey);
            if (guildConfig == default)
                guildConfig = await this.CreateAsync(guild);

            return guildConfig;
        }

        public Task<Boolean> ExistsAsync(IGuild guild)
        {
            var guildConfigKey = $"guildConfig:{guild.Id}";

            return this._db.ExistsAsync(guildConfigKey);
        }

        public async Task AddBlacklistedUser(IGuild guild, IUser user)
        {
            var blacklistKey = $"guildConfig:{guild.Id}:userBlacklist";

            await this._db.Database.SetAddAsync(blacklistKey, user.Id);
        }

        public async Task RemoveBlacklistUser(IGuild guild, IUser user)
        {
            var blacklistKey = $"guildConfig:{guild.Id}:userBlacklist";

            await this._db.Database.SetRemoveAsync(blacklistKey, user.Id);
        }

        public Task<Boolean> IsUserBlacklisted(IGuild guild, IUser user)
        {
            var blacklistKey = $"guildConfig:{guild.Id}:userBlacklist";

            return this._db.Database.SetContainsAsync(blacklistKey, user.Id);
        }

        public async Task<IEnumerable<UInt64>> GetBlacklistedUsers(IGuild guild)
        {
            var blacklistKey = $"guildConfig:{guild.Id}:userBlacklist";

            var blacklistedUsers = await this._db.Database.SetMembersAsync(blacklistKey);

            return blacklistedUsers.Select(v => (UInt64) v);
        }
    }
}

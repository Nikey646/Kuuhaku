using System;
using System.Threading.Tasks;
using Discord;
using Kuuhaku.Database;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Classes;
using Microsoft.EntityFrameworkCore;
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
    }
}

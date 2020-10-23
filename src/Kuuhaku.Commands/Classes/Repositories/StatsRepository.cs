using System;
using System.Threading.Tasks;
using Discord;
using Kuuhaku.Commands.Models;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Kuuhaku.Commands.Classes.Repositories
{
    public class StatsRepository
    {
        private IRedisDatabase _db;

        public StatsRepository(IRedisCacheClient redis)
        {
            this._db = redis.Db0;
        }

        public async Task<Stats> CreateGuildStatsAsync(IGuild guild)
        {
            var stats = new Stats();

            await this.UpdateGuildStatsAsync(guild, stats);

            return stats;
        }

        public Task UpdateGuildStatsAsync(IGuild guild, Stats stats)
        {
            var statsKey = $"stats:{guild.Id}";

            return this._db.AddAsync(statsKey, stats);
        }

        public async Task<Stats> GetGuildStatsAsync(IGuild guild)
        {
            var statsKey = $"stats:{guild.Id}";

            var exists = await this._db.ExistsAsync(statsKey);
            if (!exists)
                return await this.CreateGuildStatsAsync(guild);

            return await this._db.GetAsync<Stats>(statsKey);
        }

        public Task IncrementGlobalCommandsAsync()
        {
            return this._db.Database.StringIncrementAsync("stats:global:commands");
        }

        public async Task<String> GetGlobalCommandsAsync()
        {
            return await this._db.Database.StringGetAsync("stats:global:commands");
        }
    }
}

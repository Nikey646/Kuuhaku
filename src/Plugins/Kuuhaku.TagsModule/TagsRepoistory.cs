using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Kuuhaku.TagsModule
{
    public class TagsRepoistory
    {
        private readonly IRedisDatabase _db;

        public TagsRepoistory(IRedisCacheClient redis)
        {
            this._db = redis.Db0;
        }

        public Task CreateAsync(IGuild guild, String name, String contents)
        {
            name = name.ToLowerInvariant();
            var tagKey = $"tags:{guild.Id}:{name}";

            return this._db.AddAsync(tagKey, contents);
        }

        public Task<String> GetAsync(IGuild guild, String name)
        {
            name = name.ToLowerInvariant();
            var tagKey = $"tags:{guild.Id}:{name}";

            return this._db.GetAsync<String>(tagKey);
        }

        public async Task<IEnumerable<String>> ListAsync(IGuild guild)
        {
            var tagPattern = $"tags:{guild.Id}:";

            var tagKeys = await this._db.SearchKeysAsync(tagPattern + "*");

            return tagKeys.Select(tk => tk.Replace(tagPattern, ""));
        }

        public Task<Boolean> ExistsAsync(IGuild guild, String name)
        {
            name = name.ToLowerInvariant();
            var tagKey = $"tags:{guild.Id}:{name}";

            return this._db.ExistsAsync(tagKey);
        }

        public Task DeleteAsync(IGuild guild, String name)
        {
            name = name.ToLowerInvariant();
            var tagKey = $"tags:{guild.Id}:{name}";

            return this._db.RemoveAsync(tagKey);
        }
    }
}

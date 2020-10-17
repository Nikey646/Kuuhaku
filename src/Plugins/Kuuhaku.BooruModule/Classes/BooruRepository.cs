using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using BooruViewer.Interop.Dtos.Booru;
using Discord.WebSocket;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Kuuhaku.BooruModule.Classes
{
    public class BooruRepository
    {
        private readonly IRedisDatabase _db;

        public BooruRepository(IRedisCacheClient redis)
        {
            this._db = redis.Db0;
        }

        public async Task UpdateLastSearchAsync(String tags, UInt64 userId, UInt64 guildId, UInt64 channelId,
            SourceBooru booru)
        {
            var tagKey = $"booruSearches:{guildId}:{channelId}:{userId}:{booru.Identifier}:tags";
            var historyKey = $"booruSearches:{guildId}:{channelId}:{userId}:{booru.Identifier}:history";

            await this._db.AddAsync(tagKey, tags);
            await this._db.Database.KeyDeleteAsync(historyKey);
        }

        public async Task UpdatePostHistoryAsync(String tags, SocketUser user, SocketGuild guild,
            ISocketMessageChannel channel, SourceBooru booru, Int64 newVal)
        {
            var tagKey = $"booruSearches:{guild.Id}:{channel.Id}:{user.Id}:{booru.Identifier}:tags";
            var historyKey = $"booruSearches:{guild.Id}:{channel.Id}:{user.Id}:{booru.Identifier}:history";

            var existingTags = await this._db.GetAsync<String>(tagKey);
            if (!String.Equals(existingTags, tags, StringComparison.InvariantCultureIgnoreCase))
                return;

            await this._db.Database.SetAddAsync(historyKey, newVal);
        }

        public async Task<ImmutableArray<Int64>> GetViewedHistoryAsync(String tags, SocketUser user, SocketGuild guild, ISocketMessageChannel channel, SourceBooru booru)
        {
            var tagKey = $"booruSearches:{guild.Id}:{channel.Id}:{user.Id}:{booru.Identifier}:tags";
            var historyKey = $"booruSearches:{guild.Id}:{channel.Id}:{user.Id}:{booru.Identifier}:history";

            var existingTags = await this._db.GetAsync<String>(tagKey);
            if (!String.Equals(existingTags, tags, StringComparison.InvariantCultureIgnoreCase))
                return ImmutableArray<Int64>.Empty;

            var history = await this._db.Database.SetMembersAsync(historyKey);
            return history.Select(v => !v.IsInteger ? (Int64) v : -1).ToImmutableArray();
        }

        public async Task UpdateSearchHistoryAsync(string tags, SocketUser user, SocketGuild guild,
            ISocketMessageChannel channel, SourceBooru booru, UInt64 newId)
        {
            var tagKey = $"booruSearches:{guild.Id}:{channel.Id}:{user.Id}:{booru.Identifier}:tags";
            var historyKey = $"booruSearches:{guild.Id}:{channel.Id}:{user.Id}:{booru.Identifier}:history";

            var existingTags = await this._db.GetAsync<String>(tagKey);
            if (!String.Equals(existingTags, tags, StringComparison.InvariantCultureIgnoreCase))
            {
                await this._db.AddAsync(tagKey, tags);
                await this._db.RemoveAsync(historyKey);
                return;
            }

            await this._db.Database.SetAddAsync(historyKey, newId);
        }
    }
}

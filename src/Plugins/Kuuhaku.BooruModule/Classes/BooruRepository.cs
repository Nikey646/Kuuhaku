using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using BooruViewer.Interop.Dtos.Booru;
using Discord;
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
            }

            await this._db.Database.SetAddAsync(historyKey, newId);
        }

        public async Task<IEnumerable<(UInt64, UInt64)>> GetSubscriptionsAsync(SourceBooru booru)
        {
            var subscriptionKey = $"booruSubscriptions:{booru.Identifier}";

            var members = await this._db.Database.SetMembersAsync(subscriptionKey);
            var idPairs = members.Select(p => p.ToString());

            var ids = new List<(UInt64, UInt64)>();

            foreach (var pair in idPairs)
            {
                var split = pair.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                    continue;

                var guildId = UInt64.Parse(split[0]);
                var channelId = UInt64.Parse(split[1]);

                ids.Add((guildId, channelId));
            }

            return ids;
        }

        public Task SubscribeAsync(SourceBooru booru, IGuild guild, IChannel channel)
        {
            var subscriptionKey = $"booruSubscriptions:{booru.Identifier}";

            return this._db.Database.SetAddAsync(subscriptionKey, $"{guild.Id},{channel.Id}");
        }

        public Task<Boolean> IsSubscribedAsync(SourceBooru booru, SocketGuild guild, IChannel channel)
        {
            var subscriptionKey = $"booruSubscriptions:{booru.Identifier}";

            return this._db.Database.SetContainsAsync(subscriptionKey, $"{guild.Id},{channel.Id}");
        }

        public Task UnsubscribeAsync(SourceBooru booru, IGuild guild, IChannel channel)
        {
            var subscriptionKey = $"booruSubscriptions:{booru.Identifier}";

            return this._db.Database.SetRemoveAsync(subscriptionKey, $"{guild.Id},{channel.Id}");
        }

        public async Task<UInt64> GetLastSubscriptionId(SourceBooru booru)
        {
            var lastIdKey = $"booruSubscriptions:{booru.Identifier}:lastId";

            var lastId = await this._db.Database.StringGetAsync(lastIdKey);

            return (UInt64) lastId;
        }

        public Task SetLastSubscriptionId(SourceBooru booru, UInt64 id)
        {
            var lastIdKey = $"booruSubscriptions:{booru.Identifier}:lastId";

            return this._db.Database.StringSetAsync(lastIdKey, id);
        }
    }
}

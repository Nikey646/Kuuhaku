using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using BooruViewer.Interop.Dtos.Booru;
using BooruViewer.Interop.Dtos.Booru.Posts;
using BooruViewer.Interop.Interfaces;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Kuuhaku.BooruModule.Modules;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.BooruModule.Classes
{
    public class SubscriptionService : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly BooruRepository _repository;
        private readonly ICollection<IBooru> _boorus;
        private readonly ILogger<SubscriptionService> _logger;
        private Dictionary<IBooru, List<(UInt64 guildId, UInt64 channelId)>> _map;

        public SubscriptionService(DiscordSocketClient client, BooruRepository repository, IEnumerable<IBooru> boorus,
            ILogger<SubscriptionService> logger)
        {
            this._client = client;
            this._repository = repository;
            this._boorus = boorus.ToImmutableList();
            this._logger = logger;
            this._map = new Dictionary<IBooru, List<(UInt64 guildId, UInt64 channelId)>>();
        }

        public async Task AddChannelAsync(IBooru booru, SocketGuild guild, IChannel channel)
        {
            var mappedBooru = this._map.First(kv => kv.Key.Booru.Identifier == booru.Booru.Identifier)
                .Key;

            this._map[mappedBooru].Add((guild.Id, channel.Id));
            await this._repository.SubscribeAsync(booru.Booru, guild, channel);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Info("Starting the Booru Subscription Service.");

            foreach (var booru in this._boorus)
            {
                if (!this._map.ContainsKey(booru))
                    this._map.Add(booru, new List<(UInt64 guildId, UInt64 channelId)>());

                var source = booru.Booru;
                var channels = await this._repository.GetSubscriptionsAsync(source);

                this._map[booru].AddRange(channels);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var tasks = new List<Task>();
                foreach (var (booru, ids) in this._map)
                {
                    if (ids.Count == 0)
                        continue;

                    this._logger.Debug($"Creating Process Task for {ids.Count} locations.");

                    tasks.Add(this.ProcessAsync(booru, ids));
                }

                if (tasks.Count > 0)
                    await Task.WhenAll(tasks);

                this._logger.Debug("All Boorus have processed new posts and posted them");
                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            this._logger.Info($"Stopping the Booru Subscription Service.");
        }

        private async Task ProcessAsync(IBooru booru, List<(UInt64 guildId, UInt64 channelId)> channelIds)
        {
            if (this._client.ConnectionState != ConnectionState.Connected)
                return; // Wait for a minute

            var lastId = await this._repository.GetLastSubscriptionId(booru.Booru);
            var newLastId = 0ul;

            var posts = await booru.GetPostsAsync("", 1, 20);
            var usablePosts = posts
                .Where(p => !(p.Files == null || p.Files.IsVideo || p.Files.IsFlash || p.Files.IsUgoira))
                .Where(p => p.Tags.All(t => !GenericBooruModule.BlacklistedTags.Contains(t.Name)))
                .OrderBy(p => p.Id)
                .SkipWhile(p => p.Id <= lastId)
                .ToImmutableArray();

            this._logger.Debug("Posting {count} new images from {booru}", usablePosts.Length, booru.Booru.Name);

            foreach (var post in usablePosts)
            {
                foreach (var (guildId, channelId) in channelIds)
                {
                    var guild = this._client.GetGuild(guildId);
                    var channel = guild.GetTextChannel(channelId);

                    if (!channel.IsNsfw && post.Rating != Rating.Safe)
                        continue;

                    var user = guild.GetUser(this._client.CurrentUser.Id);

                    await channel.SendMessageAsync(this.CreatePostEmbed(post, booru.Booru, user));
                }


                newLastId = post.Id;
            }

            if (newLastId == 0)
                return;

            await this._repository.SetLastSubscriptionId(booru.Booru, newLastId);
        }

        private KuuhakuEmbedBuilder CreatePostEmbed(Post post, SourceBooru sauce, IUser botUser)
        {
            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithAuthor($"Id - {post.Id}", $"{sauce.BaseUri}favicon.ico", $"{sauce.BaseUri}posts/{post.Id}")
                .WithTimestamp(post.UploadedAt)
                .WithFooter(botUser);

            var preview = $"{post.Files.Preview}";

            return embed
                .WithFieldIf("Source", () => post.Source.Href.IsEmpty()
                        ? post.Source.FriendlyName
                        : $"[{post.Source.FriendlyName}]({post.Source.Href})",
                    includeIf: post.Source != null)
                .WithField("Rating", post.Rating.ToString())
                .AddField(this.CreateTagsField(post.Tags, sauce))
                .WithImageUrl(preview);
        }

        private EmbedFieldBuilder CreateTagsField(ICollection<Tag> tags, SourceBooru sauce)
        {
            String PostUrl(String booruId)
            {
                return booruId switch
                {
                    "yandere" => "post",
                    "konachan" => "post",
                    _ => "posts",
                };
            }

            String EscapeLink(String input)
                => input.Replace("[", "\\[")
                    .Replace("]", "\\]")
                    .Replace("(", "%28")
                    .Replace(")", "%29")
                    .Replace("_", "\\_");

            String Linkify(String name, String url)
            {
                return $"[{name}]({url})";
            }

            var builder = new EmbedFieldBuilder()
                .WithName("Tags")
                .WithIsInline(false);

            var length = 0;
            var transformedTags = new List<String>();

            foreach (var tag in tags
                .OrderBy(t => (Int32) t.Type)
                .ThenBy(t => t.Name))
            {
                var mdLink = Linkify(CustomTitleCaseTransformer.Instance.Transform(tag.Name),
                    $"{sauce.BaseUri}{PostUrl(sauce.Identifier)}?tags={EscapeLink(tag.Name.UrlEncode())}");

                length += mdLink.Length + 2; // +2 for ", " after each.
                if (length >= 1000) // Leave 24 characters spare if this condition is true.
                {
                    transformedTags.Add("More…");
                    break;
                }

                transformedTags.Add(mdLink);
            }

            return builder.WithValue(transformedTags.Humanize());
        }
    }
}

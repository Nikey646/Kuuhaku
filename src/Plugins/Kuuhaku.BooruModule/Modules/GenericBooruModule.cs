using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using BooruViewer.Interop.Dtos.Booru;
using BooruViewer.Interop.Dtos.Booru.Posts;
using BooruViewer.Interop.Interfaces;
using Discord;
using Discord.Commands;
using Humanizer;
using Kuuhaku.BooruModule.Classes;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.BooruModule.Modules
{
    public abstract class GenericBooruModule : KuuhakuModule
    {
        private readonly BooruRepository _repository;
        private readonly String[] _blacklistedTags;

        public abstract IBooru Booru { get; }

        public GenericBooruModule(BooruRepository repository)
        {
            this._repository = repository;

            this._blacklistedTags = new[]
            {
                "loli", "lolicon",
                "shota", "shotacon",
                "gore", "guro",
                "vore",
                "toddlercon",
                "rape",
            };
        }

        [Command]
        public virtual async Task GetPostsAsync(String tags = "")
        {
            using var typing = this.Channel.EnterTypingState();

            var textChannel = this.Channel as ITextChannel;

            // If for some reason we're not in a text channel, assume SFW only.
            var isNsfw = textChannel?.IsNsfw ?? false;

            var viewHistory = await
                this._repository.GetViewedHistoryAsync(tags, this.User, this.Guild, this.Channel, this.Booru.Booru);
            var page = 1ul;
            Post post = null;
            while (post == null)
            {
                if (page > 5)
                    break;

                var allPosts = await this.Booru.GetPostsAsync(tags, page++, 20);
                var usablePosts = allPosts
                    // These types of posts are not viable for viewing in Discord.
                    .Where(p => !(p.Files == null || p.Files.IsVideo || p.Files.IsFlash || p.Files.IsUgoira))
                    .Where(p => isNsfw || p.Rating == Rating.Safe)
                    .Where(p => p.Tags.All(t => !this._blacklistedTags.Contains(t.Name)))
                    // It's safe to cast UInt64 p.Id to Int64 due to the fact the id will never exceed the max size of Int64.
                    .Where(p => !viewHistory.Contains((Int64) p.Id))
                    .ToImmutableArray();

                post = usablePosts.FirstOrDefault();
            }

            if (post == null)
            {
                await this.ReplyAsync("Maximum of 5 pages reached.");
                return;
            }

            await this.ReplyAsync(this.CreatePostEmbed(post, this.Booru.Booru));
            await this._repository.UpdateSearchHistoryAsync(tags, this.User, this.Guild, this.Channel, this.Booru.Booru,
                post.Id);
        }

        private KuuhakuEmbedBuilder CreatePostEmbed(Post post, SourceBooru sauce)
        {
            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithAuthor($"Id - {post.Id}", $"{sauce.BaseUri}favicon.ico", $"{sauce.BaseUri}posts/{post.Id}")
                .WithTimestamp(post.UploadedAt)
                .WithFooter(this.Context);

            var preview = $"{post.Files.Preview}";
            var original = $"{post.Files.Original}";

            var fileSizeValue = "";

            try
            {
                var fileSizeAsInt = (Int32) post.Files.FileSize;
                fileSizeValue = fileSizeAsInt.Bytes().ToString("0.##");
            }
            catch
            {
                fileSizeValue = "Too big to count!";
            }

            return embed
                .WithField("Hash", post.Hash)
                .WithField("Size", fileSizeValue)
                .WithFieldIf("Download", () => $"[Full Image]({original})", includeIf: original != preview)
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
                    $"{sauce.BaseUri}posts?tags={EscapeLink(tag.Name.UrlEncode())}");

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

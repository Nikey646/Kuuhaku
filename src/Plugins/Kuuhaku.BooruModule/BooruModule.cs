using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BooruViewer.Interop.Boorus;
using BooruViewer.Interop.Dtos.Booru;
using BooruViewer.Interop.Dtos.Booru.Posts;
using Discord;
using Discord.Commands;
using Humanizer;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.BooruModule
{
    public class BooruModule : KuuhakuModule
    {
        private static Uri _baseUrl = new Uri("https://danbooru.donmai.us/");
        private readonly Danbooru _danbooru;

        public BooruModule(Danbooru danbooru)
        {
            this._danbooru = danbooru;
        }

        [Command("danbooru")]
        public async Task GetPostsAsync(String tags = "", UInt64 page = 1)
        {
            using var typing = this.Channel.EnterTypingState();

            var posts = await this._danbooru.GetPostsAsync(tags, page, 10);
            var firstPost = posts.First(p => p.Rating == Rating.Safe);

            await this.ReplyAsync(this.CreatePostEmbed(firstPost));
        }

        private KuuhakuEmbedBuilder CreatePostEmbed(Post post)
        {
            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithAuthor($"Id - {post.Id}", $"{_baseUrl}favicon.ico", $"{_baseUrl}posts/{post.Id}")
                .WithTimestamp(post.UploadedAt)
                .WithFooter(this.Context);

            var preview = $"{post.Files.Preview}";
            var original = $"{post.Files.Original}";
            var source = post.Source.Href.IsEmpty()
                ? post.Source.FriendlyName
                : $"[{post.Source.FriendlyName}]({post.Source.Href})";

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
                .WithField("Source", source)
                .WithField("Rating", post.Rating.ToString())
                .AddField(this.CreateTagsField(post.Tags))
                .WithImageUrl(preview);
        }

        private EmbedFieldBuilder CreateTagsField(ICollection<Tag> tags)
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
                var mdLink = Linkify(CustomTitleCaseTransformer.Instance.Transform(tag.Name), $"{_baseUrl}posts?tags={EscapeLink(tag.Name.UrlEncode())}");

                length += mdLink.Length + 2;
                if (length >= 1024)
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

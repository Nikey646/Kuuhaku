using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Kuuhaku.Commands.Attributes;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.TagsModule
{
    [Group("tag"), Alias("tags")]
    public class TagsModule : KuuhakuModule
    {
        private readonly TagsRepoistory _repoistory;

        public TagsModule(TagsRepoistory repoistory)
        {
            this._repoistory = repoistory;
        }

        [Command, Priority(2)]
        public async Task ListAsync()
        {
            var tagsEnumerable = await this._repoistory.ListAsync(this.Guild);
            var tags = tagsEnumerable.ToImmutableArray();

            if (tags.Length == 0)
            {
                await this.ReplyAsync(this.Embed.WithDescription($"There are no tags available in this server."));
                return;
            }

            await this.ReplyAsync(this.Embed.WithDescription($"The tags available are: {tags.Humanize()}"));
        }

        [Command, Priority(1)]
        public async Task ViewAsync(String name)
        {
            var tag = await this._repoistory.GetAsync(this.Guild, name);

            if (tag.IsEmpty())
            {
                // TODO: Find similar.
                await this.ReplyAsync(this.Embed.WithDescription($"There is no such tag as {name.MdBold().Quote()}"));
                return;
            }

            await this.ReplyAsync(this.Embed.WithTitle(name).WithDescription(tag));
        }

        [Command("create"), Alias("create", "add", "add"), RequiredMinPermission(CommandPermissions.Moderator), Priority(3)]
        public async Task CreateAsync(String name, String contents)
        {
            var exists = await this._repoistory.ExistsAsync(this.Guild, name);
            if (exists)
            {
                await this.ReplyAsync(
                    this.Embed.WithDescription($"There is already a tag with the name {name.MdBold().Quote()}"));
                return;
            }

            await this._repoistory.CreateAsync(this.Guild, name, contents);

            var prefixMessage = $"Created a new tag with the name {name.MdBold().Quote()} and the description:\n";
            var isTooLong = contents.Length + prefixMessage.Length > EmbedBuilder.MaxDescriptionLength;
            await this.ReplyAsync(this.Embed.WithDescription(prefixMessage +
                                                             (isTooLong
                                                                 ? contents.Substring(0,
                                                                     contents.Length - prefixMessage.Length - 2) + "…"
                                                                 : contents)));
        }

        [Command("delete"), Alias("remove"), RequiredMinPermission(CommandPermissions.Moderator), Priority(3)]
        public async Task DeleteAsync(String name)
        {
            var exists = await this._repoistory.ExistsAsync(this.Guild, name);
            if (!exists)
            {
                await this.ReplyAsync(
                    this.Embed.WithDescription($"There is no such tag with the name {name.MdBold().Quote()}"));
                return;
            }

            await this._repoistory.DeleteAsync(this.Guild, name);
            await this.ReplyAsync(
                this.Embed.WithDescription($"The tag with the name of {name.MdBold().Quote()} was deleted."));
        }
    }
}

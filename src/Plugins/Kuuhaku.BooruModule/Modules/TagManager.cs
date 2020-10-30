using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Discord.Commands;
using Humanizer;
using Kuuhaku.BooruModule.Classes;
using Kuuhaku.Commands.Attributes;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.BooruModule.Modules
{
    public class TagManager : KuuhakuModule
    {
        private readonly BooruRepository _booruRepository;

        public TagManager(BooruRepository booruRepository)
        {
            this._booruRepository = booruRepository;
        }

        [Command("booru blacklist"), RequiredMinPermission(CommandPermissions.BotOwner)]
        public async Task ToggleBlacklistAsync(String tag, String reason)
        {
            // normalize.
            tag = tag.ToLowerInvariant().Trim();
            reason = reason.ToLowerInvariant().Trim();

            var blacklisted = await this._booruRepository.BlacklistContainsAsync(tag, reason);

            if (blacklisted)
            {
                await this._booruRepository.BlacklistRemoveAsync(tag, reason);
                await this.ReplyAsync(this.Embed.WithDescription($"{tag.MdBold().Quote()} (for {reason.Quote()}) is no longer blacklisted."));
            }
            else
            {
                await this._booruRepository.BlacklistAddAsync(tag, reason);
                await this.ReplyAsync(this.Embed.WithDescription($"{tag.MdBold().Quote()} is now blacklisted because {reason.Quote()}."));
            }
        }

        [Command("booru blacklist list"), RequiredMinPermission(CommandPermissions.BotOwner)]
        public async Task ListBlacklistAsync()
        {
            var blacklistedTags = await this._booruRepository.BlacklistGetAllAsync();

            var response =
                this.Embed
                    .WithDescription(blacklistedTags.Humanize(dto => $"{dto.Tag.MdBold()} ({dto.Reason})"))
                    .WithField("Total Tags Blacklisted", blacklistedTags.Length);

            await this.ReplyAsync(response);
        }

    }
}

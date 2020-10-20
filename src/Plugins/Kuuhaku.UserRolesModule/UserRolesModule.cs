using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Attributes;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.UserRolesModule.Services;

namespace Kuuhaku.UserRolesModule
{
    public class UserRolesModule : KuuhakuModule
    {
        private readonly UserRoleService _userRoleService;

        public UserRolesModule(UserRoleService userRoleService)
        {
            this._userRoleService = userRoleService;
        }

        [Command("user roles add"), RequiredMinPermission(CommandPermissions.Moderator)]
        public Task AddRoleAsync(IRole role, IEmote emote, [Remainder] String shortDescription)
            => this.AddRoleAsync(this.Channel, role, emote, shortDescription);

        [Command("user roles add"), RequiredMinPermission(CommandPermissions.Moderator)]
        public async Task AddRoleAsync(IChannel channel, IRole role, IEmote emote, [Remainder] String shortDescription)
        {
            if (this.IsPrivate)
            {
                var builder= new KuuhakuEmbedBuilder()
                    .WithTitle("No no")
                    .WithDescription("This command is only for servers. Sorry.")
                    .WithFooter(this.Client.CurrentUser)
                    .WithCurrentTimestamp();
                await this.ReplyAsync(builder);
                return;
            }

            if (!(channel is IMessageChannel messageChannel))
            {
                var builder = new KuuhakuEmbedBuilder()
                    .WithTitle("Woopsies")
                    .WithDescription("Channel provided does not appear to be a text channel.")
                    .WithFooter(this.Context)
                    .WithCurrentTimestamp();
                await this.ReplyAsync(builder);
                return;
            }

            await this._userRoleService.AddRoleAsync(this.Guild, messageChannel, role, emote, shortDescription);
            await this.Message.AddReactionAsync(new Emoji(NeoSmart.Unicode.Emoji.ThumbsUp.ToString()));
        }

        [Command("user roles remove"), RequiredMinPermission(CommandPermissions.Moderator)]
        public Task RemoveRoleAsync(IRole role)
            => this.RemoveRoleAsync(this.Channel, role);

        [Command("user roles remove"), RequiredMinPermission(CommandPermissions.Moderator)]
        public async Task RemoveRoleAsync(IMessageChannel channel, IRole role)
        {
            if (this.IsPrivate)
            {
                var builder = new KuuhakuEmbedBuilder()
                    .WithColor()
                    .WithTitle("No no")
                    .WithDescription("This command is only for servers. Sorry.")
                    .WithFooter(this.Context);
                await this.ReplyAsync(builder);
                return;
            }

            await this._userRoleService.RemoveRoleAsync(this.Guild, channel, role);
            await this.Message.AddReactionAsync(new Emoji(NeoSmart.Unicode.Emoji.ThumbsUp.ToString()));
        }
    }
}

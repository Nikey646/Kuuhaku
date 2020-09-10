using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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

        [Command("user roles add")]
        public Task AddRoleAsync(IRole role, IEmote emote, [Remainder] String shortDescription)
            => this.AddRoleAsync(this.Channel, role, emote, shortDescription);

        [Command("user roles add")]
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
        }
    }
}

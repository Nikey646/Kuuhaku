using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Commands.Modules
{
    [Group("permissions")]
    public class PermissionsModule : KuuhakuModule
    {
        private readonly PermissionsRepository _repository;

        public PermissionsModule(PermissionsRepository repository)
        {
            this._repository = repository;
        }

        [Command("moderator")]
        public async Task ToggleModeratorAsync(IRole role)
        {
            var isMod = await this._repository.ExistsAsync(this.Guild, CommandPermissions.Moderator.ToString(), role);
            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithFooter(this.Context);

            if (isMod)
            {
                await this._repository.RemoveRoleAsync(this.Guild, CommandPermissions.Moderator.ToString(), role);
                await this.ReplyAsync(embed.WithDescription($"{role.Mention} is no longer classified as a moderator"));
            }
            else
            {
                await this._repository.AddRoleAsync(this.Guild, CommandPermissions.Moderator.ToString(), role);
                await this.ReplyAsync(embed.WithDescription($"{role.Mention} is now classified as a moderator"));
            }
        }

        [Command("admin")]
        public async Task ToggleAdminAsync(IRole role)
        {
            var isMod = await this._repository.ExistsAsync(this.Guild, CommandPermissions.Admin.ToString(), role);
            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithFooter(this.Context);

            if (isMod)
            {
                await this._repository.RemoveRoleAsync(this.Guild, CommandPermissions.Admin.ToString(), role);
                await this.ReplyAsync(embed.WithDescription($"{role.Mention} is no longer classified as an admin"));
            }
            else
            {
                await this._repository.AddRoleAsync(this.Guild, CommandPermissions.Admin.ToString(), role);
                await this.ReplyAsync(embed.WithDescription($"{role.Mention} is now classified as an admin"));
            }
        }

    }
}

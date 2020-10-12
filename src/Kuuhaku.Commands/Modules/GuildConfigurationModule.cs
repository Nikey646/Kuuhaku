using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Models;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Commands.Modules
{
    [Group("config"), RequireContext(ContextType.Guild)]
    public class GuildConfigurationModule : KuuhakuModule
    {
        private readonly GuildConfigRepository _repository;

        public GuildConfigurationModule(GuildConfigRepository repository)
        {
            this._repository = repository;
        }

        [Command("prefix")]
        public async Task GetPrefixAsync()
        {
            var config = await this.GetGuildConfigAsync();
            if (config == default)
            {
                await this.ReplyAsync(
                    "Failed to find the configuration for your guild. Please report this to the bot owner.");
                return;
            }

            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithDescription($"The prefix for this server is {config.Prefix.MdBold()}")
                .WithFooter(this.Context);
            await this.ReplyAsync(embed);
        }

        [Command("prefix")]
        public async Task SetPrefixAsync(String newPrefix)
        {
            var config = await this.GetGuildConfigAsync();
            if (config == default)
            {
                await this.ReplyAsync(
                    "Failed to find the configuration for your guild. Please report this to the bot owner.");
                return;
            }

            var oldPrefix = config.Prefix;
            config.Prefix = newPrefix;
            await this._repository.Context.SaveChangesAsync();

            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithDescription($"The prefix for this server is now {config.Prefix.MdBold()} (Was previously {oldPrefix.MdBold()})")
                .WithFooter(this.Context);
            await this.ReplyAsync(embed);
        }

        private async Task<GuildConfig> GetGuildConfigAsync()
        {
            var configs = await this._repository.FindAsync(c => c.GuildId == this.Guild.Id);
            return configs.FirstOrDefault();
        }
    }
}

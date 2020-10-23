using System.Threading.Tasks;
using Discord.Commands;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Commands.Modules
{
    public class StatsModule : KuuhakuModule
    {
        private readonly StatsRepository _repository;

        public StatsModule(StatsRepository repository)
        {
            this._repository = repository;
        }

        [Command("stats")]
        public async Task StatsAsync()
        {
            var stats = await this._repository.GetGuildStatsAsync(this.Guild);
            var globalCommands = await this._repository.GetGlobalCommandsAsync();

            var embed = this.Embed
                .WithDescription("Global and Server Stats")
                .WithField("Global Commands Executed", globalCommands)
                .WithField("Server Commands Executed", stats.CommandsExecuted)
                .WithField("Messages Seen", stats.MessagesSeen);

            await this.ReplyAsync(embed);
        }

    }
}

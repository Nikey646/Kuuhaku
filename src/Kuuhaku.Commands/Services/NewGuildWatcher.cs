using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Database;
using Kuuhaku.Database.DbModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.Commands.Services
{
    public class NewGuildWatcher : IHostedService
    {
        private readonly GuildConfigRepository _repository;
        private readonly DiscordSocketClient _discordClient;

        public NewGuildWatcher(GuildConfigRepository repository, DiscordSocketClient discordClient)
        {
            this._repository = repository;
            this._discordClient = discordClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._discordClient.GuildAvailable += this.OnGuildAvailableAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._discordClient.GuildAvailable += this.OnGuildAvailableAsync;
            return Task.CompletedTask;
        }

        private Task OnGuildAvailableAsync(SocketGuild guild)
        {
            Task.Factory.StartNew(() => this.GuildAvailableAsync(guild).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        private async Task GuildAvailableAsync(SocketGuild guild)
        {
            var configExists = await this._repository.ExistsAsync(guild);
            if (!configExists)
            {
                await this._repository.CreateAsync(guild);
            }
        }
    }
}

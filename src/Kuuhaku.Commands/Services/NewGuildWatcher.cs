using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Kuuhaku.Database;
using Kuuhaku.Database.DbModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.Commands.Services
{
    public class NewGuildWatcher : IHostedService
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly IServiceProvider _serviceProvider;

        public NewGuildWatcher(DiscordSocketClient discordClient, IServiceProvider serviceProvider)
        {
            this._discordClient = discordClient;
            this._serviceProvider = serviceProvider;
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
            Task.Factory.StartNew(async () => await this.GuildAvailableAsync(guild).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        private async Task GuildAvailableAsync(SocketGuild guild)
        {
            using var scope = this._serviceProvider.CreateScope();
            await using var dbContext = scope.ServiceProvider.GetService<DisgustingGodContext>();

            var set = dbContext.GuildConfigs;

            // TODO: Should this be cached in memory to prevent excessive database polling
            // in the event of guilds becoming available again?

            if (set.Any(g => g.GuildId == guild.Id))
                return;

            var guildConfig = new GuildConfig(guild.Id);
            await set.AddAsync(guildConfig);
            await dbContext.SaveChangesAsync();
        }
    }
}

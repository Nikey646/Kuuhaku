using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.Commands.Services
{
    public class StatsService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly StatsRepository _repository;
        private readonly ILogger<StatsService> _logger;
        private readonly PrefixCommandHandler _commandHandler;

        public StatsService(DiscordSocketClient client, PrefixCommandHandler commandHandler, StatsRepository repository, ILogger<StatsService> logger)
        {
            this._client = client;
            this._commandHandler = commandHandler;
            this._repository = repository;
            this._logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.Info("Starting Stats Service");
            this._client.MessageReceived += this.OnMessageReceivedAsync;
            this._commandHandler.CommandSucceeded += this.OnCommandSuccessfullyExecutedAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.Info("Stoppingg Stats Service");
            this._commandHandler.CommandSucceeded -= this.OnCommandSuccessfullyExecutedAsync;
            this._client.MessageReceived -= this.OnMessageReceivedAsync;
            return Task.CompletedTask;
        }

        private async Task OnCommandSuccessfullyExecutedAsync(KuuhakuCommandContext context, ExecuteResult result)
        {
            await this._repository.IncrementGlobalCommandsAsync();

            if (context.IsPrivate)
                return;

            var guildStats = await this._repository.GetGuildStatsAsync(context.Guild);
            guildStats.CommandsExecuted++;
            await this._repository.UpdateGuildStatsAsync(context.Guild, guildStats);
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            if (!(message is SocketUserMessage userMessage))
                return;

            if (!(userMessage.Channel is SocketGuildChannel channel))
                return;

            var guildStats = await this._repository.GetGuildStatsAsync(channel.Guild);
            guildStats.MessagesSeen++;
            await this._repository.UpdateGuildStatsAsync(channel.Guild, guildStats);
        }

        private Task OnMessageReceivedAsync(SocketMessage message)
        {
            Task.Factory.StartNew(() => this.MessageReceivedAsync(message).ConfigureAwait(false));
            return Task.CompletedTask;
        }
    }
}

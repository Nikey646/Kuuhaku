using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.Gateway;

namespace Kuuhaku.Services
{
    public class DiscordService : BackgroundService
    {
        private readonly DiscordGatewayClient _client;
        private readonly ILogger<DiscordService> _logger;

        public DiscordService(DiscordGatewayClient client, ILogger<DiscordService> logger)
        {
            this._client = client;
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var runResult = await this._client.RunAsync(stoppingToken);

                if (!runResult.IsSuccess)
                {
                    this._logger.LogError(runResult.Exception, runResult.ErrorReason);

                    if (runResult.GatewayCloseStatus.HasValue)
                    {
                        this._logger.LogError("Gateway close status: {gatewayClosedStatus}",
                            runResult.GatewayCloseStatus.Value);
                    }

                    if (runResult.WebSocketCloseStatus.HasValue)
                    {
                        this._logger.LogError("Websocket close status: {websocketCloseStatus}",
                            runResult.WebSocketCloseStatus.Value);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                var previous = (GatewayConnectionStatus) (typeof(DiscordGatewayClient)
                    .GetField("_connectionStatus", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(this._client) ?? 0);
                this._logger.LogTrace("Setting DiscordGatewayClient._connectionStatus to Offline was {previous}, workaround Nihlus/Remora.Discord#23", previous.ToString());
                typeof(DiscordGatewayClient).GetField("_connectionStatus", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(this._client, 0);
            }
        }
    }
}

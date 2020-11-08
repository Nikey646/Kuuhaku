using System;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Services;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Gateway.Extensions;

namespace Kuuhaku.Extensions
{
    public static class HostingExtensions
    {
        public static IHostBuilder UseDiscord(this IHostBuilder builder, Func<HostBuilderContext, String> tokenFunc)
        {
            return builder.ConfigureServices((ctx, services) => services
                .AddHostedServiceSingleton<DiscordService>()
                .AddDiscordGateway(() => tokenFunc(ctx)));
        }
    }
}

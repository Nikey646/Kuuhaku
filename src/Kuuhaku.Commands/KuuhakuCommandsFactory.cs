using System;
using System.IO;
using Discord.Commands;
using Kuuhaku.Commands.Classes;
using Kuuhaku.Commands.Classes.ModuleMetadataProviders;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Commands.Options;
using Kuuhaku.Commands.Services;
using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.Commands
{
    public class KuuhakuCommandsFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.Configure<CommandHandlerOptions>(ctx.Configuration.GetSection("CommandHandler"));
            services.AddSingleton<IInteractionService, InteractionService>();

            services.AddSingleton<CustomModuleBuilder>();
            services.AddSingleton<IModuleBuilder>(s => s.GetRequiredService<CustomModuleBuilder>());
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<CustomModuleBuilder>());

            services.AddSingleton<IHostedService, NewGuildWatcher>();

            services.AddSingleton<IModuleMetadataProvider>(s => new ChainableProvider(s.GetRequiredService<ILogger<ChainableProvider>>())
                .AddProvider(new JsonProvider(Path.Combine(AppContext.BaseDirectory, "Metadata"),
                    s.GetRequiredService<ILogger<JsonProvider>>()))
                .AddProvider(s.GetRequiredService<AttributeProvider>()));

            services.AddSingleton(s =>
                new CommandService(s.GetRequiredService<CommandServiceConfig>()));

            services.AddSingleton<RepeatRepository>();
            services.AddSingleton<GuildConfigRepository>();
            services.AddSingleton<PermissionsRepository>();
            services.AddSingleton<StatsRepository>();

            services.AddSingleton<PrefixCommandHandler>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<PrefixCommandHandler>());

            services.AddSingleton<StatsService>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<StatsService>());
        }

        (String configKey, Object defaultValue) IPluginFactory.ConfigureDefaultConfiguration() =>
            ("CommandHandler", new CommandHandlerOptions());
    }
}

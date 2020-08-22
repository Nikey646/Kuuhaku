using System;
using Kuuhaku.Commands.Options;
using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.Commands
{
    public class KuuhakuCommandsFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.Configure<CommandHandlerOptions>(ctx.Configuration.GetSection("CommandHandler"));
            services.AddHostedService<PrefixCommandHandler>();
        }

        (String configKey, Object defaultValue) IPluginFactory.ConfigureDefaultConfiguration() =>
            ("CommandHandler", new CommandHandlerOptions());
    }
}

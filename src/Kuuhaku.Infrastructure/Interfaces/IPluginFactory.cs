using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kuuhaku.Infrastructure.Interfaces
{
    public interface IPluginFactory
    {

        void ConfigureServices(HostBuilderContext ctx, IServiceCollection services);

        (String configKey, Object defaultValue) ConfigureDefaultConfiguration()
        {
            var name = this.GetType().Name
                .Replace("Kūhaku", "")
                .Replace("Kuuhaku", "")
                .Replace("Factory", "");
            return (name, null);
        }

        Task LoadDiscordModulesAsync(IModuleBuilder moduleBuilder)
        {
            var currentType = this.GetType();
            Log.ForContext<IPluginFactory>().Verbose("Loading Modules provided via {currentType}", currentType.Name);
            return moduleBuilder.BuildAsync(currentType.Assembly);
        }

    }
}

using Kuuhaku.Infrastructure.Interfaces;
using Kuuhaku.ReminderModule.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.ReminderModule
{
    public class ReminderModuleFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddSingleton<IHostedService, ReminderService>();
            // services.AddHostedService<ReminderService>();
        }
    }
}

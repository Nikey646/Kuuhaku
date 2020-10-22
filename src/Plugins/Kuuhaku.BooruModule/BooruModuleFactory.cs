using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BooruViewer.Interop.Extensions;
using Kuuhaku.BooruModule.Classes;

namespace Kuuhaku.BooruModule
{
    public class BooruModuleFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.WithAllBoorus();

            services.AddSingleton<SubscriptionService>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<SubscriptionService>());
        }
    }
}

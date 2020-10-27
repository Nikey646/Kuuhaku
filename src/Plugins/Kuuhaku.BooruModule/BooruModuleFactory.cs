using System;
using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BooruViewer.Interop.Extensions;
using Kuuhaku.BooruModule.Classes;
using Kuuhaku.BooruModule.Models;

namespace Kuuhaku.BooruModule
{
    public class BooruModuleFactory : IPluginFactory
    {
        public (String configKey, Object defaultValue) ConfigureDefaultConfiguration()
        {
            return ("boorus", new {danbooru = new {username = "", apiKey = ""}});
        }

        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.WithAllBoorus();

            services.AddSingleton<SubscriptionService>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<SubscriptionService>());

            services.Configure<BooruOptions>("danbooru", ctx.Configuration.GetSection("boorus:Danbooru"));
        }
    }
}

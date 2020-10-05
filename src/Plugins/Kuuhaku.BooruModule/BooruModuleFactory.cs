using System;
using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BooruViewer.Interop.Extensions;

namespace Kuuhaku.BooruModule
{
    public class BooruModuleFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.WithDanbooru();
        }
    }
}

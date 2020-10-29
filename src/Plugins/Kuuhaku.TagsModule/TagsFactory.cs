using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.TagsModule
{
    public class TagsFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddSingleton<TagsRepoistory>();
        }
    }
}

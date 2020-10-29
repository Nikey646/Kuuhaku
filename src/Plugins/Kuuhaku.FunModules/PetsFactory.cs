using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.FunModules
{
    // Secretly a kitty / puppy mill...?
    public class PetsFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        { }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.Infrastructure.Interfaces
{
    public interface IPluginFactory
    {
        void ConfigureServices(HostBuilderContext ctx, IServiceCollection services);

        Task RegisterResolvers();

        (String configKey, Object defaultValue) ConfigureDefaultConfiguration()
        {
            var name = this.GetType().Name
                .Replace("Kūhaku", "")
                .Replace("Kuuhaku", "")
                .Replace("Factory", "");
            return (name, null);
        }
    }
}

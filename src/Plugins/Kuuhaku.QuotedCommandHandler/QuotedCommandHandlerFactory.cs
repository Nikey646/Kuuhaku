using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.QuotedCommandHandler
{
    public class QuotedCommandHandlerFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddHostedService<QuotedCommandHandler>();
        }
    }
}

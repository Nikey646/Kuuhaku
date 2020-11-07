using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedServiceSingleton<TServiceType>(this IServiceCollection services)
            where TServiceType : class, IHostedService
            => services.AddSingleton<TServiceType>()
                .AddSingleton<IHostedService, TServiceType>(s => s.GetRequiredService<TServiceType>());
    }
}

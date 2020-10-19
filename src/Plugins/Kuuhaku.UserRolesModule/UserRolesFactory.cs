using System;
using Kuuhaku.Infrastructure.Interfaces;
using Kuuhaku.UserRolesModule.Classes;
using Kuuhaku.UserRolesModule.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.UserRolesModule
{
    public class UserRolesFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            // services.AddScoped<UserRolesUoW>();
            // services.AddScoped<UserRolesRepository>();

            services.AddSingleton<UserRolesRepository>();

            services.AddSingleton<UserRoleService>();
            services.AddSingleton<IHostedService, UserRoleService>(s => s.GetRequiredService<UserRoleService>());

            // services.AddHostedService<UserRoleService>();
        }
    }
}

using System;
using Kuuhaku.Database.Services;
using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;

namespace Kuuhaku.Database
{
    public class KuuhakuDatabaseFactory : IPluginFactory
    {
        public void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddDbContext<DisgustingGodContext>(o =>
            {
                o.EnableDetailedErrors(ctx.HostingEnvironment.IsDevelopment())
                    .EnableSensitiveDataLogging(ctx.HostingEnvironment.IsDevelopment())
                    .UseMySql(ctx.Configuration.GetConnectionString("DefaultConnection"), oo =>
                        oo.ServerVersion(new Version(10, 5), ServerType.MariaDb)
                            .CharSet(CharSet.Utf8Mb4)
                            .CharSetBehavior(CharSetBehavior.AppendToAllColumns));
            });

            services.AddHostedService<DatabaseMigrationService>();
        }
    }
}

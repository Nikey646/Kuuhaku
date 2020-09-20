using System;
using System.Threading;
using System.Threading.Tasks;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.Database.Services
{
    public class DatabaseMigrationService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(IServiceProvider services, ILogger<DatabaseMigrationService> logger)
        {
            this._services = services;
            this._logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.Info("Running Database Migrations");
            using var scope = this._services.CreateScope();
            using var dbContext = scope.ServiceProvider.GetService<DisgustingGodContext>();
            await dbContext.Database.MigrateAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

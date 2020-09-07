using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;

namespace Kuuhaku.Database
{
    internal class DesignTimeContextCreator : IDesignTimeDbContextFactory<DisgustingGodContext>
    {
        public DisgustingGodContext CreateDbContext(String[] args)
        {
            var optsBuilder = new DbContextOptionsBuilder<DisgustingGodContext>();
            optsBuilder.EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .UseMySql("Server=127.0.0.1;Database=Kuuhaku;Uid=root;Pwd=example;", oo =>
                    oo.ServerVersion(new Version(10, 5), ServerType.MariaDb)
                        .CharSet(CharSet.Utf8Mb4)
                        .CharSetBehavior(CharSetBehavior.AppendToAllColumns));

            return new DisgustingGodContext(optsBuilder.Options);
        }
    }
}

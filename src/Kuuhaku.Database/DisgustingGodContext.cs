using System.Diagnostics;
using Kuuhaku.Database.DbModels;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Kuuhaku.Database
{
    public class DisgustingGodContext : DbContext
    {
        public DbSet<GuildConfig> GuildConfigs { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        // For EF Core migration tools.
        public DisgustingGodContext(DbContextOptions<DisgustingGodContext> opts) : base(opts)
        { }
    }
}

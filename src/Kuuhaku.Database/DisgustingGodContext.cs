using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serilog;

namespace Kuuhaku.Database
{
    public class DisgustingGodContext : DbContext
    {
        public DbSet<GuildConfig> GuildConfigs { get; set; }
        public DbSet<Reminder> Reminders { get; set; }


        public DbSet<UserRoleLocation> UserRoleLocations { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // For EF Core migration tools.
        public DisgustingGodContext(DbContextOptions<DisgustingGodContext> opts) : base(opts)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UserRole>()
                .Property(nameof(UserRole.ShortDescription))
                .HasMaxLength(200);
        }
    }
}

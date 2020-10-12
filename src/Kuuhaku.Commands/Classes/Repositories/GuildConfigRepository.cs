using System;
using Kuuhaku.Database;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Classes;
using Microsoft.EntityFrameworkCore;

namespace Kuuhaku.Commands.Classes.Repositories
{
    public class GuildConfigRepository : AbstractRepository<GuildConfig, Guid>
    {
        public GuildConfigRepository(DisgustingGodContext context) : base(context)
        { }
    }
}

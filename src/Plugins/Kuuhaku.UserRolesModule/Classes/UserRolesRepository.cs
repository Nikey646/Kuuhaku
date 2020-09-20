using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kuuhaku.Database;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Classes;
using Microsoft.EntityFrameworkCore;

namespace Kuuhaku.UserRolesModule.Classes
{
    public class UserRolesRepository : AbstractRepository<UserRoleLocation, Guid>, IDisposable
    {
        public new readonly DisgustingGodContext Context;

        public UserRolesRepository(DisgustingGodContext context) : base(context)
        {
            this.Context = context;
        }

        public new Task<UserRoleLocation> GetAsync(Guid id)
            => this.Context.UserRoleLocations
                .Include(url => url.Roles)
                .FirstOrDefaultAsync(url => url.Id == id);

        public override Task<List<UserRoleLocation>> FindAsync(Expression<Func<UserRoleLocation, Boolean>> predicate)
            => this.Context.UserRoleLocations
                .Include(url => url.Roles)
                .Where(predicate)
                .ToListAsync();

        public override Task<List<UserRoleLocation>> GetAllAsync()
            => this.Context.UserRoleLocations
                .Include(url => url.Roles)
                .ToListAsync();
    }
}

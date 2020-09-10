using System;
using Kuuhaku.Database;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Classes;
using Microsoft.EntityFrameworkCore;

namespace Kuuhaku.UserRolesModule.Classes
{
    public class UserRolesRepository : AbstractRepository<UserRoleLocation, Guid>, IDisposable
    {
        private readonly DisgustingGodContext _context;

        public UserRolesRepository(DisgustingGodContext context) : base(context)
        {
            this._context = context;
        }
    }
}

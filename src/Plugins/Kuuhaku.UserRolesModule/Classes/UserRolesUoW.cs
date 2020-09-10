using Kuuhaku.Database;
using Kuuhaku.Infrastructure.Classes;

namespace Kuuhaku.UserRolesModule.Classes
{
    public class UserRolesUoW : UnitOfWork<DisgustingGodContext>
    {

        public UserRolesUoW(DisgustingGodContext context, UserRolesRepository userRolesRepository) : base(context)
        {
            this.UserRoles = userRolesRepository;
        }

        public UserRolesRepository UserRoles { get; }
    }
}

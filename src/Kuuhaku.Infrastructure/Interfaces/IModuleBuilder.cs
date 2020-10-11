using System.Reflection;
using System.Threading.Tasks;

namespace Kuuhaku.Infrastructure.Interfaces
{
    public interface IModuleBuilder
    {
        Task BuildAsync(params Assembly[] assemblies);
    }
}

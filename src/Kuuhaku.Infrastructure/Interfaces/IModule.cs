using Discord.Commands;
using Kuuhaku.Infrastructure.Models;

namespace Kuuhaku.Infrastructure.Interfaces
{
    public interface IModule
    {
        void SetContext(KuuhakuCommandContext context);
        void BeforeExecute(CommandInfo command);
        void AfterExecute(CommandInfo command);
    }
}

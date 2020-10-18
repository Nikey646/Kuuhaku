using Discord.Commands;
using Kuuhaku.Commands.Models;

namespace Kuuhaku.Commands.Interfaces
{
    public interface IModule
    {
        void SetContext(KuuhakuCommandContext context);
        void BeforeExecute(CommandInfo command);
        void AfterExecute(CommandInfo command);
    }
}

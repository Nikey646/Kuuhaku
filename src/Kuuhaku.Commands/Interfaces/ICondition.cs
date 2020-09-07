using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Kuuhaku.Commands.Interfaces
{
    public interface ICondition<T>
    {
        Task<Boolean> ValidateAsync(ICommandContext context, T param);
    }
}

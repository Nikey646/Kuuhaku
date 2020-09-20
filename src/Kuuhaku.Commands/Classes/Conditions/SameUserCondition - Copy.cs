using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Interfaces;

namespace Kuuhaku.Commands.Classes.Conditions
{
    public class SameUserCondition : ICondition<IMessage>
    {
        public Task<Boolean> ValidateAsync(ICommandContext context, IMessage param)
        {
            return Task.FromResult(context.User.Id == param.Author.Id);
        }
    }
}

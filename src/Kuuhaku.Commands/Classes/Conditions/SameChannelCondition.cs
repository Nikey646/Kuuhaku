using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Interfaces;

namespace Kuuhaku.Commands.Classes.Conditions
{
    public class SameChannelCondition : ICondition<IMessage>
    {
        public Task<Boolean> ValidateAsync(ICommandContext context, IMessage param)
        {
            return Task.FromResult(context.Channel.Id == param.Channel.Id);
        }
    }
}

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Interfaces;

namespace Kuuhaku.Commands.Classes.Conditions
{
    public class ChannelCondition : ICondition<IMessage>
    {
        private readonly UInt64 _channelId;

        public ChannelCondition(IMessageChannel channel)
        {
            this._channelId = channel.Id;
        }

        public Task<Boolean> ValidateAsync(ICommandContext context, IMessage param)
        {
            return Task.FromResult(this._channelId == param.Channel.Id);
        }
    }
}

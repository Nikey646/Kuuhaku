using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Interfaces;

namespace Kuuhaku.Commands.Classes.Conditions
{
    public class UserCondition : ICondition<IMessage>
    {
        private readonly UInt64 _userId;

        public UserCondition(IUser user)
        {
            this._userId = user.Id;
        }

        public Task<Boolean> ValidateAsync(ICommandContext context, IMessage param)
        {
            return Task.FromResult(this._userId == param.Author.Id);
        }
    }
}

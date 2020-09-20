using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Kuuhaku.Commands.Interfaces
{
    public interface IInteractionService
    {
        Task<SocketMessage> NextMessageAsync(ICommandContext context, Boolean sameUser = true,
            Boolean sameChannel = true, TimeSpan timeout = default, CancellationToken ct = default);
        Task<SocketMessage> NextMessageAsync(ICommandContext context, ICondition<IMessage> condition,
            TimeSpan timeout, CancellationToken ct = default);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Commands.Classes.Conditions;
using Kuuhaku.Commands.Interfaces;

namespace Kuuhaku.Commands.Services
{
    public class InteractionService : IInteractionService
    {
        public Task<SocketMessage> NextMessageAsync(ICommandContext context, Boolean sameUser = true, Boolean sameChannel = true,
            TimeSpan timeout = default, CancellationToken ct = default)
        {
            var condition = new ChainableCondition<IMessage>();
            if (sameUser)
                condition.AddCondition(new SameUserCondition());
            if (sameChannel)
                condition.AddCondition(new SameChannelCondition());

            return this.NextMessageAsync(context, condition, timeout, ct);
        }

        public async Task<SocketMessage> NextMessageAsync(ICommandContext context, ICondition<IMessage> condition, TimeSpan timeout,
            CancellationToken ct = default)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Timeout can not be Zero", nameof(timeout));

            var eventTrigger = new TaskCompletionSource<SocketMessage>();
            var cancelTrigger = new TaskCompletionSource<Boolean>();

            ct.Register(() => cancelTrigger.SetResult(true));

            async Task Handler(SocketMessage message)
            {
                var result = await condition.ValidateAsync(context, message);
                if (result)
                    eventTrigger.SetResult(message);
            }

            if (!(context.Client is BaseSocketClient socketClient))
                throw new ArgumentException("The Discord Client for the provided command context does not implement BaseSocketClient.", nameof(context.Client));

            socketClient.MessageReceived += Handler;

            var trigger = eventTrigger.Task;
            var cancel = cancelTrigger.Task;
            var delay = Task.Delay(timeout);

            var task = await Task.WhenAny(trigger, delay, cancel);

            socketClient.MessageReceived -= Handler;

            if (task == trigger)
                return await trigger;
            return null;
        }
    }
}

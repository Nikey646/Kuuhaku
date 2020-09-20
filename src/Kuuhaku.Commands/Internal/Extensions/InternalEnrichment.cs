using System;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Internal.Enrichers;
using Serilog.Context;

namespace Kuuhaku.Commands.Internal.Extensions
{
    internal static class InternalEnrichment
    {
        public static IDisposable Enrich(this ICommandContext context) =>
            LogContext.Push(new CommandContextEnricher(context));

        internal static void Deconstruct(this ICommandContext context, out IUser user, out IUserMessage message,
            out IGuild guild, out IMessageChannel channel)
        {
            user = context.User;
            message = context.Message;
            guild = context.Guild;
            channel = context.Channel;
        }
    }
}

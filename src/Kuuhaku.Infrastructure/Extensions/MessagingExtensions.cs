using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Models;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class MessagingExtensions
    {

        public static Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, String message,
            KuuhakuEmbedBuilder embedBuilder, CancellationToken ct = default)
            => channel.SendMessageAsync(message, embed: embedBuilder.Build(),
                options: new RequestOptions {CancelToken = ct});

        public static Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, KuuhakuEmbedBuilder embedBuilder, CancellationToken ct = default)
            => channel.SendMessageAsync("", embedBuilder, ct);

    }
}
